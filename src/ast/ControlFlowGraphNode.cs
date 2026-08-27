using System;
using System.Text;

namespace TableScript;

abstract partial class CFGNode{
	static int counter;
	public int id {get; private init;}
	public bool isEntry; //Prevent total removal
	
	public CFGNode(){
		id = counter++;
	}
	
	public List<CFGNode> entries = new();
	
	public bool replace(CFGNode r){
		if(r == this){
			return false;
		}
		
		if(entries.Count == 0){
			return false;
		}
		
		foreach(CFGNode entry in entries.ToArray()){
			entry.replaceChild(this, r);
		}
		
		return true;
	}
	
	public virtual void replaceChild(CFGNode o, CFGNode r){
		
	}
	
	public virtual void remove(){
		
	}
	
	public void entriesRemove(CFGNode n){
		entries.Remove(n);
		if(entries.Count == 0 && !isEntry){
			remove();
		}
	}
	
	public virtual CFGNode[] successors(){
		return Array.Empty<CFGNode>();
	}
	
	public static string ToString(CFGNode p){
		StringBuilder sb = new();
		HashSet<CFGNode> seen = new();
		
		Stack<CFGNode> pending = new();
		pending.Push(p);
		
		while(pending.Count > 0){
			CFGNode n = pending.Pop();
			if(n != null && seen.Add(n)){
				sb.AppendLine(n.ToString());
				
				switch(n){
					case StmtCFGNode s:
						pending.Push(s.next);
						break;
					
					case CondCFGNode c:
						pending.Push(c.isFalse);
						pending.Push(c.isTrue);
						break;
				}
			}
		}
		
		return sb.ToString();
	}
}

class StmtCFGNode : CFGNode{
	public Stmt[] statements;
	
	CFGNode _next;
	public CFGNode next {get => _next; set{
		CFGNode old = _next;
		_next = value;
		if(_next != null){
			_next.entries.Add(this);
		}
		if(old != null){
			old.entriesRemove(this);
		}
	}}
	
	public StmtCFGNode(Stmt[] s) : base(){
		statements = s;
	}
	
	public StmtCFGNode(Stmt s) : base(){
		if(s is BlockStmt b){
			statements = b.inner;
		}else{
			statements = new Stmt[]{s};
		}
	}
	
	public override void replaceChild(CFGNode o, CFGNode r){
		if(next == o){
			next = r;
		}
	}
	
	public override void remove(){
		next = null;
	}
	
	public override CFGNode[] successors(){
		if(next != null){
			return new CFGNode[]{next};
		}else{
			return Array.Empty<CFGNode>();
		}
	}
	
	public override string ToString(){
		return "#" + id +
			(entries.Count > 0 ? " {ENTRIES: " + string.Join(", ", entries.Select(e => "#" + e.id)) + "}" : "") +
			" [\n" + string.Join("\n", statements.Select(s => "\t" + s?.ToString())) + "\n]" +
			"NEXT: #" + next?.id;
	}
}

class CondCFGNode : CFGNode{
	public Expr condition;
	
	CFGNode _isTrue;
	public CFGNode isTrue {get => _isTrue; set{
		CFGNode old = _isTrue;
		_isTrue = value;
		if(_isTrue != null){
			_isTrue.entries.Add(this);
		}
		if(old != null){
			old.entriesRemove(this);
		}
	}}
	
	CFGNode _isFalse;
	public CFGNode isFalse {get => _isFalse; set{
		CFGNode old = _isFalse;
		_isFalse = value;
		if(_isFalse != null){
			_isFalse.entries.Add(this);
		}
		if(old != null){
			old.entriesRemove(this);
		}
	}}
	
	public CondCFGNode(Expr c) : base(){
		condition = c;
	}
	
	public override void replaceChild(CFGNode o, CFGNode r){
		if(isTrue == o){
			isTrue = r;
		}
		
		if(isFalse == o){
			isFalse = r;
		}
	}
	
	public override void remove(){
		isTrue = null;
		isFalse = null;
	}
	
	public override CFGNode[] successors(){
		if(isTrue != null){
			return isFalse == null ? new CFGNode[]{isTrue} : new CFGNode[]{isTrue, isFalse};
		}else{
			return isFalse != null ? new CFGNode[]{isFalse} : Array.Empty<CFGNode>();
		}
	}
	
	public override string ToString(){
		return "#" + id +
		(entries.Count > 0 ? " {ENTRIES: " + string.Join(", ", entries.Select(e => "#" + e.id)) + "}" : "") +
		" IF <" + condition.ToString() + ">\n\tTRUE: #" + isTrue?.id + "\n\tFALSE: #" + isFalse?.id;
	}
}

class DummyCFGNode : CFGNode{	
	public override string ToString(){
		return "#" + id + " DUMMY" +
			(entries.Count > 0 ? " {ENTRIES: " + string.Join(", ", entries.Select(e => "#" + e.id)) + "}" : "");
	}
}

class RedirectCFGNode : CFGNode{	
	public override string ToString(){
		return "#" + id + " REDIRECT" +
			(entries.Count > 0 ? " {ENTRIES: " + string.Join(", ", entries.Select(e => "#" + e.id)) + "}" : "");
	}
}

class CFGFragment{
	public CFGNode entry;
	public CFGNode exit;
	
	public CFGFragment(CFGNode e, CFGNode x){
		entry = e;
		exit = x;
	}
}