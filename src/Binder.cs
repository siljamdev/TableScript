using System;

namespace TabScript;

//Purpose of this class: transforming variables into indices, functions into indices, and cutting unused functions
class Binder{
	public Action<TabScriptException> OnReport;
	public bool hadError{get; private set;}
	
	Scope glob = new Scope(null);
	Scope currScope;
	
	string currentImport;
	string currentFilename;
	
	//break, continue, return illegal usage outside of loop
	bool checkingLoop;
	bool checkingFunction;
	
	Stack<TabFunc> funcsBeingChecked = new();
	
	Snippet main; //Main code
	Snippet[] bodies; //Secondary code
	
	//All functions
	TabFunc[] allFuncs;
	
	//Functions currently available
	TabFunc[] funcs;
	
	Dictionary<string, string[]> symbols;
	
	//Functions that are used and are therefore kept
	List<TabFunc> funcsFinal = new();
	
	public Binder(ResolvedScript resolved){
		main = resolved.mainBody;
		bodies = resolved.bodies;
		allFuncs = resolved.allFunctions;
		symbols = resolved.availableImports;
	}
	
	void updateImport(string import){
		if(currentImport == import){
			return;
		}
		currentImport = import;
		funcs = allFuncs.Where(f => (f.import == currentImport) || (f.export && symbols[currentImport].Contains(f.import))).ToArray();
	}
	
	public TableScript Bind(){
		List<Stmt> body = new(main.body.Length);
		
		currScope = glob;
		currScope.define(main.filename, 0, main.import + "::args"); //args variable
		
		//Secondary bodies
		foreach(Snippet sec in bodies){
			currentFilename = sec.filename;
			updateImport(sec.import);
			currScope = glob;
			
			for(int i = 0; i < sec.body.Length; i++){
				try{
					Stmt st = Bind(sec.body[i]);
					if(st != null){
						body.Add(st);
					}
				}catch(TabScriptException e){
					hadError = true;
					OnReport?.Invoke(e);
				}
			}
		}
		
		currentFilename = main.filename;
		updateImport(main.import);
		currScope = glob;
		
		for(int i = 0; i < main.body.Length; i++){
			try{
				Stmt st = Bind(main.body[i]);
				if(st != null){
					body.Add(st);
				}
			}catch(TabScriptException e){
				hadError = true;
				OnReport?.Invoke(e);
			}
		}
		
		//Prevent unused functions from adding new functions to funcsFinal
		TabFunc[] funcsFinalCopy = funcsFinal.ToArray();
		
		//Not continue with errors in unused functions
		for(int i = 0; i < allFuncs.Length; i++){
			try{
				Bind(allFuncs[i]);
			}catch(TabScriptException e){
				hadError = true;
				OnReport?.Invoke(e);
			}
		}
		
		if(hadError){			
			throw new TabScriptException(TabScriptErrorType.Binder, main.filename, -1, "Errors present: Unable to continue");
			return null;
		}else{
			return new TableScript(new Snippet(main.filename, main.import, body.ToArray()), funcsFinalCopy);
		}
	}
	
	Stmt Bind(Stmt p){
		switch(p){
			//Changes
			case VarDeclStmt k:				
				Expr v = Bind(k.val, p.line);
				
				(int d, int i) = currScope.define(currentFilename, p.line, currentImport + "::" + k.identifier);
				
				return new OptVarDeclStmt(d, i, v, p.line);
			
			//Changes
			case TabAssignStmt a:
				v = Bind(a.val, p.line);
				
				(d, i) = currScope.assign(currentFilename, p.line, currentImport + "::" + a.identifier);
				
				return new OptTabAssignStmt(d, i, v, p.line);
			
			//Changes
			case ElementAssignStmt l:
				v = Bind(l.val, p.line);
				
				(d, i) = currScope.assign(currentFilename, p.line, currentImport + "::" + l.identifier);
				IndexExpr idd2 = (IndexExpr) Bind(l.ind, p.line);
				
				return new OptElementAssignStmt(d, i, idd2, v, p.line);
			
			case ExprStmt e:
				return new ExprStmt(Bind(e.exp, p.line), p.line);
			
			case BlockStmt b:
				currScope = new Scope(currScope);
				
				Stmt[] ne = b.inner.Select(h => Bind(h)).ToArray();
				
				currScope = currScope.parent;
				
				return new BlockStmt(ne, p.line);
			
			case IfStmt f:
				return new IfStmt(Bind(f.condition, p.line), Bind(f.then), Bind(f.els), p.line);
			
			case WhileStmt w:
				Expr cond = Bind(w.condition, p.line);
				
				checkingLoop = true;
				Stmt bod = Bind(w.body);
				checkingLoop = false;
				
				Stmt els = Bind(w.els);
				
				return new WhileStmt(cond, bod, els, p.line);
			
			case ForeachStmt t:
				Expr pool = Bind(t.pool, p.line);
				
				checkingLoop = true;
				currScope = new Scope(currScope);
				
				currScope.define(currentFilename, p.line, currentImport + "::" + t.id);
				
				ne = t.body.inner.Select(h => Bind(h)).ToArray();
				
				currScope = currScope.parent;
				checkingLoop = false;
				
				BlockStmt body = new BlockStmt(ne, t.body.line);
				
				els = Bind(t.els);
				
				return new ForeachStmt(t.id, pool, body, els, p.line);
			
			case DoStmt du:
				cond = Bind(du.condition, p.line);
				
				checkingLoop = true;
				bod = Bind(du.body);
				checkingLoop = false;
				
				els = Bind(du.els);
				
				return new DoStmt(cond, bod, els, p.line);
			
			case BreakStmt:
				if(!checkingLoop){
					throw new TabScriptException(TabScriptErrorType.Binder, currentFilename, p.line, "Break statement outside of loop");
				}
				return p;
			
			case ContinueStmt:
				if(!checkingLoop){
					throw new TabScriptException(TabScriptErrorType.Binder, currentFilename, p.line, "Continue statement outside of loop");
				}
				return p;
			
			case ReturnStmt r:
				if(!checkingFunction){
					Console.WriteLine(Environment.StackTrace);
					throw new TabScriptException(TabScriptErrorType.Binder, currentFilename, p.line, "Return statement outside of function");
				}
				return new ReturnStmt(Bind(r.val, p.line), p.line);
			
			default:
				return p;
		}
	}
	
	TabFunc Bind(TabFunc p){
		switch(p){
			case TabNativeFunc f:
				if(f.pars.Length != f.pars.Distinct().Count()){
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Function parameters must not repeat names");
				}
				
				if(allFuncs.Any(h => !ReferenceEquals(h, f) && f.SameSignature(h))){
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Functions must have different signatures: '" + f.import + "::" + f.identifier + "'");
				}
				
				Scope tempScope = currScope;
				currScope = new Scope(glob);
				
				bool checkingFunctionTemp = checkingFunction;
				checkingFunction = true;
				funcsBeingChecked.Push(f);
				
				string temp2 = currentFilename;
				string temp3 = currentImport;
				currentFilename = p.filename;
				updateImport(f.import);
				
				foreach(string p222 in f.pars){ //define parameters
					currScope.define(currentFilename, f.line, currentImport + "::" + p222);
				}
				Stmt[] ne = f.body.inner.Select(h => Bind(h)).ToArray();
				
				currentFilename = temp2;
				updateImport(temp3);
				
				checkingFunction = checkingFunctionTemp;
				funcsBeingChecked.Pop();
				
				currScope = tempScope;
				
				return new TabNativeFunc(f.import, f.identifier, f.pars, f.self, f.export, new BlockStmt(ne, f.body.line), p.filename, p.line);
			
			case TabExternFunc x:
				if(x.pars.Length != x.pars.Distinct().Count()){
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Function parameters must not repeat names");
				}
				
				if(allFuncs.Any(h => !ReferenceEquals(h, x) && x.SameSignature(h))){
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Function must have different signatures: " + x.import + "::" + x.identifier);
				}
				
				return p;
			
			default:
				return p;
		}
	}
	
	Expr Bind(Expr p, int line){
		switch(p){
			//Changes
			case CallExpr c:
				string cimport = c.import == "local" ? currentImport : c.import;
				
				TabFunc fx = null;
				if(cimport == null){ //Try match local first
					fx = Array.Find(funcs, f => f.Matches(currentImport, c.identifier, c.arity));
				}
				
				if(fx == null){
					fx = Array.Find(funcs, f => f.Matches(cimport, c.identifier, c.arity)); //Match in available functions
					if(fx == null){
						throw new TabScriptException(TabScriptErrorType.Binder, currentFilename, line, "No function available with '" + (cimport == null ? "" : cimport + "::") + c.identifier + "' as identifier and " + c.arity + " parameters");
					}
				}
				
				if(c.self && !fx.self){
					throw new TabScriptException(TabScriptErrorType.Binder, currentFilename, line, "The function '" + fx.import + "::" + fx.identifier + "' is not a self function.");
				}
				
				//get its index in finals
				int fxind = funcsFinal.FindIndex(f => fx.SameSignature(f));
				if(fxind < 0){ //Not found in finals
					if(!funcsBeingChecked.Any(f => ReferenceEquals(f, fx))){ //Prevent infinite loops
						funcsFinal.Add(Bind(fx)); //Add it
						fxind = funcsFinal.Count - 1;
					}else{
						fxind = funcsFinal.Count + funcsBeingChecked.ToList().FindIndex(f => ReferenceEquals(f, fx));
					}
				}
				
				//Bind arguments
				Expr[] n3 = c.args.Select(h => Bind(h, line)).ToArray();
				
				return new OptCallExpr(fxind, n3);
			
			//Changes
			case VariableExpr v:
				(int d2, int i) = currScope.get(currentFilename, line, currentImport + "::" + v.identifier);
				return new OptVariableExpr(d2, i);
			
			case BinaryExpr b:
				Expr o1 = Bind(b.left, line);
				Expr o2 = Bind(b.right, line);
				
				return new BinaryExpr(o1, b.op, o2);
			
			case UnaryExpr u:
				o1 = Bind(u.right, line);
				return new UnaryExpr(u.op, o1);
			
			case TernaryExpr q:
				o1 = Bind(q.cond, line);
				o2 = Bind(q.tr, line);
				Expr o3 = Bind(q.fa, line);
				return new TernaryExpr(o1, o2, o3);
			
			case GroupingExpr g:
				o1 = Bind(g.exp, line);
				return new GroupingExpr(o1);
			
			case GetElementExpr e:
				o1 = Bind(e.left, line);
				IndexExpr idd2 = (IndexExpr) Bind(e.ind, line);
				return new GetElementExpr(o1, idd2);
			
			case GetRangeExpr r:
				o1 = Bind(r.left, line);
				IndexExpr idd = (IndexExpr) Bind(r.ind, line);
				IndexExpr lld = (IndexExpr) Bind(r.len, line);
				return new GetRangeExpr(o1, idd, lld);
			
			case IndexExpr indxx:
				return indxx.val == null ? indxx : new IndexExpr(default, Bind(indxx.val, line));
			
			case BuildLiteralExpr d:
				Expr[] n = d.parts.Select(h => Bind(h, line)).ToArray();
				return new BuildLiteralExpr(n);
			
			case DollarExpr r:
				Expr[] n2 = r.parts.Select(h => Bind(h, line)).ToArray();
				return new DollarExpr(n2);
			
			default:
				return p;
		}
	}
}