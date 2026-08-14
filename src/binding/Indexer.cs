using System;

namespace TabScript;

//Purpose of this class: transforming variable uids into real stack indexes
class Indexer{
	Allocator alloc;
	
	Snippet main; //Main code
	
	//All functions
	TabFunc[] funcs;
	
	Optimizations opt;
	
	public Indexer(BindedScript ts, Optimizations opt){
		main = ts.body;
		funcs = ts.functions;
		alloc = ts.allocator;
		this.opt = opt;
	}
	
	public TableScript Index(){
		alloc.startIndexing(opt);
		
		Stmt[] body = new Stmt[main.body.Length];
		
		for(int i = 0; i < main.body.Length; i++){
			body[i] = Index(main.body[i]);
		}
		
		for(int i = 0; i < funcs.Length; i++){
			Index(funcs[i]);
		}
		
		return new TableScript(new Snippet(main.filename, main.import, body.ToArray()), funcs);
	}
	
	Stmt Index(Stmt p){
		switch(p){			
			//Changes
			case OptVarAssignStmt a:
				Expr v = Index(a.val, p.line);
				
				return new OptVarAssignStmt(alloc.getIndex(a.index), v, p.line);
			
			//Changes
			case OptElementAssignStmt l:
				v = Index(l.val, p.line);
				
				IndexExpr idd2 = (IndexExpr) Index(l.ind, p.line);
				
				return new OptElementAssignStmt(alloc.getIndex(l.index), idd2, v, p.line);
			
			case ExprStmt e:
				return new ExprStmt(Index(e.exp, p.line), p.line);
			
			case BlockStmt b:
				Stmt[] ne = b.inner.Select(h => Index(h)).ToArray();
				
				return new BlockStmt(ne, p.line);
			
			case IfStmt f:
				return new IfStmt(Index(f.condition, p.line), Index(f.then), Index(f.els), p.line);
			
			case WhileStmt w:
				Expr cond = Index(w.condition, p.line);
				
				Stmt bod = Index(w.body);
				
				Stmt els = Index(w.els);
				
				return new WhileStmt(cond, bod, els, p.line);
			
			case OptForeachStmt t:
				Expr pool = Index(t.pool, p.line);
				
				ne = t.body.inner.Select(h => Index(h)).ToArray();
				
				BlockStmt body = new BlockStmt(ne, t.body.line);
				
				els = Index(t.els);
				
				return new OptForeachStmt(alloc.getIndex(t.index), pool, body, els, p.line);
			
			case DoStmt du:
				cond = Index(du.condition, p.line);
				
				bod = Index(du.body);
				
				els = Index(du.els);
				
				return new DoStmt(cond, bod, els, p.line);
			
			case ReturnStmt r:
				return new ReturnStmt(Index(r.val, p.line), p.line);
			
			default:
				return p;
		}
	}
	
	//Modifies it
	void Index(TabFunc p){
		if(p is TabNativeFunc f){
			Stmt[] ne = f.body.inner.Select(h => Index(h)).ToArray();
			
			f.body = new BlockStmt(ne, f.body.line);
		}
	}
	
	Expr Index(Expr p, int line){
		switch(p){			
			//Changes
			case OptVariableExpr v:
				return new OptVariableExpr(alloc.getIndex(v.index));
			
			case OptCallExpr c:
				Expr[] n3 = c.args.Select(h => Index(h, line)).ToArray();
				
				return new OptCallExpr(c.index, n3);
			
			case BinaryExpr b:
				Expr o1 = Index(b.left, line);
				Expr o2 = Index(b.right, line);
				
				return new BinaryExpr(o1, b.op, o2);
			
			case UnaryExpr u:
				o1 = Index(u.right, line);
				return new UnaryExpr(u.op, o1);
			
			case TernaryExpr q:
				o1 = Index(q.cond, line);
				o2 = Index(q.tr, line);
				Expr o3 = Index(q.fa, line);
				return new TernaryExpr(o1, o2, o3);
			
			case GetElementExpr e:
				o1 = Index(e.left, line);
				IndexExpr idd2 = (IndexExpr) Index(e.ind, line);
				return new GetElementExpr(o1, idd2);
			
			case GetRangeExpr r:
				o1 = Index(r.left, line);
				IndexExpr idd = (IndexExpr) Index(r.ind, line);
				IndexExpr lld = (IndexExpr) Index(r.len, line);
				return new GetRangeExpr(o1, idd, lld);
			
			case IndexExpr indxx:
				return indxx.val == null ? indxx : new IndexExpr(default, Index(indxx.val, line));
			
			case BuildLiteralExpr d:
				Expr[] n = d.parts.Select(h => Index(h, line)).ToArray();
				return new BuildLiteralExpr(n);
			
			default:
				return p;
		}
	}
}