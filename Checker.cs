using System;

namespace TabScript;

class Checker{
	static void report(TabScriptException x){
		Console.Error.WriteLine(x.ToShortString());
	}
	
	public Action<TabScriptException> OnReport = report;
	
	Scope glob = new Scope(null);
	
	Scope currScope;
	
	bool hadError;
	
	bool checkingLoop;
	
	bool checkingFunction;
	
	TabFunc[] funcs;
	
	public Checker(){
		glob.define(0, "args");
	}
	
	public TableScript Check(TableScript s){
		funcs = s.functions;
		
		hadError = false;
		
		//Main
		currScope = glob;
		
		Stmt[] s2 = new Stmt[s.topLevel.Length];
		
		for(int i = 0; i < s.topLevel.Length; i++){
			try{
				s2[i] = Check(s.topLevel[i]);
			}catch(TabScriptException e){
				hadError = true;
				OnReport(e);
			}
		}
		
		TabFunc[] fs = new TabFunc[s.functions.Length];
		
		//Other funcs
		for(int i = 0; i < s.functions.Length; i++){
			try{
				fs[i] = Check(s.functions[i]);
			}catch(TabScriptException e){
				hadError = true;
				OnReport(e);
			}
		}
		
		if(hadError){
			glob = new Scope(null);
			glob.define(0, "args");
			
			throw new TabScriptException(TabScriptErrorType.Checker, -1, "Errors present: Unable to continue");
		}
		
		return hadError ? null : new TableScript(s2, fs);
	}
	
	Stmt Check(Stmt p){
		switch(p){
			case ExprStmt e:
				return new ExprStmt(Check(e.exp, p.line), p.line);
			
			case BlockStmt b:
				currScope = new Scope(currScope);
				
				Stmt[] ne = b.inner.Select(h => Check(h)).ToArray();
				
				currScope = currScope.parent;
				
				return new BlockStmt(ne, p.line);
			
			case VarDeclStmt k:				
				Expr v = Check(k.val, p.line);
				
				(int d, int i) = currScope.define(p.line, k.identifier);
				
				return new OptVarDeclStmt(d, i, v, p.line);
			
			case TabAssignStmt a:
				v = Check(a.val, p.line);
				
				(d, i) = currScope.assign(p.line, a.identifier);
				
				return new OptTabAssignStmt(d, i, v, p.line);
			
			case ElementAssignStmt l:
				v = Check(l.val, p.line);
				
				(d, i) = currScope.assign(p.line, l.identifier);
				
				return new OptElementAssignStmt(d, i, l.ind, v, p.line);
			
			case IfStmt f:
				return new IfStmt(Check(f.condition, p.line), Check(f.then), Check(f.els), p.line);
			
			case WhileStmt w:
				Expr cond = Check(w.condition, p.line);
				
				checkingLoop = true;
				Stmt bod = Check(w.body);
				checkingLoop = false;
				
				Stmt els = Check(w.els);
				
				return new WhileStmt(cond, bod, els, p.line);
			
			case ForeachStmt t:
				Expr pool = Check(t.pool, p.line);
				
				checkingLoop = true;
				currScope = new Scope(currScope);
				
				currScope.define(p.line, t.id);
				
				ne = t.body.inner.Select(h => Check(h)).ToArray();
				
				currScope = currScope.parent;
				checkingLoop = false;
				
				BlockStmt body = new BlockStmt(ne, t.body.line);
				
				els = Check(t.els);
				
				return new ForeachStmt(t.id, pool, body, els, p.line);
			
			case DoStmt du:
				cond = Check(du.condition, p.line);
				
				checkingLoop = true;
				bod = Check(du.body);
				checkingLoop = false;
				
				els = Check(du.els);
				
				return new DoStmt(cond, bod, els, p.line);
			
			case BreakStmt:
				if(!checkingLoop){
					throw new TabScriptException(TabScriptErrorType.Checker, p.line, "Break statement outside of loop");
				}
				return p;
			
			case ContinueStmt:
				if(!checkingLoop){
					throw new TabScriptException(TabScriptErrorType.Checker, p.line, "Continue statement outside of loop");
				}
				return p;
			
			case ReturnStmt r:
				if(!checkingFunction){
					throw new TabScriptException(TabScriptErrorType.Checker, p.line, "Return statement outside of function");
				}
				return new ReturnStmt(Check(r.val, p.line), p.line);
			
			default:
				return p;
		}
	}
	
	TabFunc Check(TabFunc p){
		switch(p){
			case TabNativeFunc f:
				if(f.pars.Length != f.pars.Distinct().Count()){
					throw new TabScriptException(TabScriptErrorType.Checker, p.line, "Function parameters must not repeat names");
				}
				
				if(funcs.Any(h => h != f && h.identifier == f.identifier && h.arity == f.arity && h.import == f.import)){
					throw new TabScriptException(TabScriptErrorType.Checker, p.line, "Function must have different signatures: " + f.identifier);
				}
				
				Scope temp = currScope;
				currScope = new Scope(glob);
				
				checkingFunction = true;
				
				foreach(string p222 in f.pars){
					currScope.define(f.line, p222);
				}
				
				Stmt[] ne = f.body.inner.Select(h => Check(h)).ToArray();
				
				checkingFunction = false;
				
				currScope = temp;
				
				return new TabNativeFunc(f.import, f.identifier, f.pars, f.self, new BlockStmt(ne, f.body.line), p.line);
			
			case TabExternFunc x:
				if(x.pars.Length != x.pars.Distinct().Count()){
					throw new TabScriptException(TabScriptErrorType.Checker, p.line, "Function parameters must not repeat names");
				}
				
				if(funcs.Any(h => h != x && h.identifier == x.identifier && h.arity == x.arity && h.import == x.import)){
					throw new TabScriptException(TabScriptErrorType.Checker, p.line, "Function must have different signatures: " + x.import + "::" + x.identifier);
				}
				
				return p;
			
			default:
				return p;
		}
	}
	
	Expr Check(Expr p, int line){
		switch(p){
			case BinaryExpr b:
				Expr o1 = Check(b.left, line);
				Expr o2 = Check(b.right, line);
				
				return new BinaryExpr(o1, b.op, o2);
			
			case UnaryExpr u:
				o1 = Check(u.right, line);
				return new UnaryExpr(u.op, o1);
			
			case TernaryExpr q:
				o1 = Check(q.cond, line);
				o2 = Check(q.tr, line);
				Expr o3 = Check(q.fa, line);
				return new TernaryExpr(o1, o2, o3);
			
			case GroupingExpr g:
				o1 = Check(g.exp, line);
				return new GroupingExpr(o1);
			
			case GetElementExpr e:
				o1 = Check(e.left, line);
				return new GetElementExpr(o1, e.ind);
			
			case GetRangeExpr r:
				o1 = Check(r.left, line);
				IndexExpr idd = (IndexExpr) Check(r.ind, line);
				IndexExpr lld = (IndexExpr) Check(r.len, line);
				return new GetRangeExpr(o1, idd, lld);
			
			case IndexExpr indxx:
				return indxx.val == null ? indxx : new IndexExpr(default, Check(indxx.val, line));
			
			case BuildLiteralExpr d:
				Expr[] n = d.parts.Select(h => Check(h, line)).ToArray();
				return new BuildLiteralExpr(n);
			
			case DollarExpr r:
				Expr[] n2 = r.parts.Select(h => Check(h, line)).ToArray();
				return new DollarExpr(n2);
			
			case CallExpr c:
				int fx = Array.FindIndex(funcs, h => h.identifier == c.identifier && h.arity == c.arity && (c.import == null ? true : h.import == c.import));
				
				if(fx < 0){
					throw new TabScriptException(TabScriptErrorType.Checker, line, "No " + (c.self ? "self " : "") + "function found with '" + c.identifier + "' as identifier and " + c.arity + " parameters");
				}
				
				if(c.self && !funcs[fx].self){
					throw new TabScriptException(TabScriptErrorType.Checker, line, "The function '" + c.identifier + "' is not a self function.");
				}
				
				Expr[] n3 = c.args.Select(h => Check(h, line)).ToArray();
				
				return new OptCallExpr(fx, n3);
			
			case VariableExpr v:
				(int d2, int i) = currScope.get(line, v.identifier);
				return new OptVariableExpr(d2, i);
			
			default:
				return p;
		}
	}
}