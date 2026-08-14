using System;

namespace TabScript;

//Purpose of this class: transforming variables into unique ids, functions into indices, and cutting unused functions, and producing variable lifetime data
class Binder{
	public Action<TabScriptException> OnReport;
	public bool hadError{get; private set;}
	
	Allocator alloc = new Allocator();
	IScope globalScope;
	IScope currScope;
	
	string currentImport;
	string currentFilename;
	
	//break, continue, return illegal usage outside of loop
	bool checkingLoop;
	bool checkingFunction;
	
	Snippet main; //Main code
	Snippet[] bodies; //Secondary code
	
	//All functions
	TabFunc[] allFuncs;
	
	//Functions currently available
	TabFunc[] funcs;
	
	Dictionary<string, Dictionary<string, string>> symbols; //Available imports and what they are called
	
	//Functions that are used and are therefore kept
	List<TabFunc> funcsFinal = new();
	
	bool removeUnusedFunctions;
	
	Optimizations opt;
	
	int programCounter = 0;
	
	public Binder(ResolvedScript resolved, Optimizations opt){
		main = resolved.mainBody;
		bodies = resolved.bodies;
		allFuncs = resolved.allFunctions;
		symbols = resolved.availableImports;
		this.opt = opt;
		
		removeUnusedFunctions = (opt & Optimizations.DeadFunctionElimination) != 0;
	}
	
	void updateImport(string import){
		if(currentImport == import){
			return;
		}
		currentImport = import;
		funcs = allFuncs.Where(f =>
			(f.import == currentImport) || //Current import
			(f.export && symbols[currentImport].Values.Contains(f.import)) //Acessibel import
		).ToArray();
	}
	
	public BindedScript Bind(){
		List<Stmt> body = new(main.body.Length);
		
		globalScope = new GlobalScope(alloc);
		globalScope.define(main.filename, 0, main.import, "args", false, programCounter); //args variable, defined here so it has index 0
		
		programCounter++;
		
		//Secondary bodies
		foreach(Snippet sec in bodies){
			currentFilename = sec.filename;
			updateImport(sec.import);
			currScope = new Scope(globalScope, alloc, currentImport);
			
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
			
			currScope.endOfLife();
		}
		
		currentFilename = main.filename;
		updateImport(main.import);
		currScope = new Scope(globalScope, alloc, main.import);
		
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
		
		currScope.endOfLife();
		
		//Prevent unused functions from adding new functions to funcsFinal
		TabFunc[] funcsFinalCopy = funcsFinal.ToArray();
		
		//Not continue with errors in unused functions // keep everything if configured
		for(int i = 0; i < allFuncs.Length; i++){
			if(funcsFinal.Contains(allFuncs[i])){
				continue;
			}
			
			try{
				funcsFinal.Add(allFuncs[i]);
				Bind(allFuncs[i], funcsFinal.Count - 1);
			}catch(TabScriptException e){
				hadError = true;
				OnReport?.Invoke(e);
			}
		}
		
		if(!removeUnusedFunctions){
			funcsFinalCopy = funcsFinal.ToArray();
		}
		
		if(hadError){			
			throw new TabScriptException(TabScriptErrorType.Binder, main.filename, -1, "Errors present: Unable to continue");
		}else{
			return new BindedScript(new Snippet(main.filename, main.import, body.ToArray()), funcsFinalCopy, alloc);
		}
	}
	
	Stmt Bind(Stmt p){
		programCounter++;
		
		switch(p){
			//Changes
			case TabDeclStmt k:				
				Expr v = Bind(k.val, p.line); //First function so you cant do tab a = a;
				
				int index = currScope.define(currentFilename, p.line, currentImport, k.identifier, false, programCounter);
				
				return new OptVarAssignStmt(index, v, p.line);
			
			//Changes
			case GlobalDeclStmt k2:				
				v = Bind(k2.val, p.line);
				
				index = globalScope.define(currentFilename, p.line, currentImport, k2.identifier, k2.export, programCounter);
				
				return new OptVarAssignStmt(index, v, p.line);
			
			//Changes
			case VarAssignStmt a:
				v = Bind(a.val, p.line);
				
				index = currScope.assign(currentFilename, p.line, currentImport, a.identifier, a.import, programCounter);
				
				return new OptVarAssignStmt(index, v, p.line);
			
			//Changes
			case ElementAssignStmt l:
				v = Bind(l.val, p.line);
				
				index = currScope.assign(currentFilename, p.line, currentImport, l.identifier, l.import, programCounter);
				IndexExpr idd2 = (IndexExpr) Bind(l.ind, p.line);
				
				return new OptElementAssignStmt(index, idd2, v, p.line);
			
			case ExprStmt e:
				return new ExprStmt(Bind(e.exp, p.line), p.line);
			
			case BlockStmt b:
				currScope = new Scope(currScope, alloc, currentImport);
				
				Stmt[] ne = b.inner.Select(h => Bind(h)).ToArray();
				
				currScope = currScope.endOfLife();
				
				return new BlockStmt(ne, p.line);
			
			case IfStmt f:
				return new IfStmt(Bind(f.condition, p.line), Bind(f.then), Bind(f.els), p.line);
			
			case WhileStmt w:
				Expr cond = Bind(w.condition, p.line);
				
				bool prev = checkingLoop;
				checkingLoop = true;
				Stmt bod = Bind(w.body);
				checkingLoop = prev;
				
				Stmt els = Bind(w.els);
				
				return new WhileStmt(cond, bod, els, p.line);
			
			case ForeachStmt t:
				Expr pool = Bind(t.pool, p.line);
				
				prev = checkingLoop;
				checkingLoop = true;
				currScope = new Scope(currScope, alloc, currentImport);
				
				int uid = currScope.define(currentFilename, p.line, currentImport, t.id, false, programCounter);
				
				ne = t.body.inner.Select(h => Bind(h)).ToArray();
				
				currScope = currScope.endOfLife();
				checkingLoop = prev;
				
				BlockStmt body = new BlockStmt(ne, t.body.line);
				
				els = Bind(t.els);
				
				return new OptForeachStmt(uid, pool, body, els, p.line);
			
			case DoStmt du:
				cond = Bind(du.condition, p.line);
				
				prev = checkingLoop;
				checkingLoop = true;
				bod = Bind(du.body);
				checkingLoop = prev;
				
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
					throw new TabScriptException(TabScriptErrorType.Binder, currentFilename, p.line, "Return statement outside of function");
				}
				return new ReturnStmt(Bind(r.val, p.line), p.line);
			
			default:
				return p;
		}
	}
	
	//Modifies it
	void Bind(TabFunc p, int index){
		switch(p){
			case TabNativeFunc f:
				if(f.pars.Length != f.pars.Distinct().Count()){
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Function parameters must not repeat names");
				}
				
				if(allFuncs.Any(h => !ReferenceEquals(h, f) && f.SameSignature(h))){ //Avoid same.signature functions
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Functions must have different signatures: '" + f.import + "::" + f.identifier + "'");
				}
				
				IScope tempScope = currScope;
				currScope = new FunctionScope(globalScope, alloc, f.import, index);
				
				bool checkingFunctionTemp = checkingFunction;
				checkingFunction = true;
				
				string temp2 = currentFilename;
				string temp3 = currentImport;
				currentFilename = p.filename;
				updateImport(f.import);
				
				foreach(string param in f.pars){ //define parameters
					currScope.define(currentFilename, f.line, currentImport, param, false, programCounter);
				}
				Stmt[] ne = f.body.inner.Select(h => Bind(h)).ToArray();
				
				currentFilename = temp2;
				updateImport(temp3);
				
				checkingFunction = checkingFunctionTemp;
				
				currScope.endOfLife();
				currScope = tempScope;
				
				f.body = new BlockStmt(ne, f.body.line);
				break;
			
			case TabExternFunc x:
				if(x.pars.Length != x.pars.Distinct().Count()){
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Function parameters must not repeat names");
				}
				
				if(allFuncs.Any(h => !ReferenceEquals(h, x) && x.SameSignature(h))){
					throw new TabScriptException(TabScriptErrorType.Binder, p.filename, p.line, "Function must have different signatures: " + x.import + "::" + x.identifier);
				}
				
				break;
		}
	}
	
	Expr Bind(Expr p, int line){
		switch(p){
			//Changes
			case CallExpr c:
				string cimport = getRealImport(c.import); //Replace local and import as (symbols)
				
				TabFunc fx = null; //First, search it in currently available functions. Then, search it in bound functions
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
				int fxind = funcsFinal.IndexOf(fx);
				if(fxind < 0){ //Not found in finals
					fxind = funcsFinal.Count;
					funcsFinal.Add(fx);
					Bind(fx, fxind);
				}
				
				//Bind arguments
				Expr[] n3 = c.args.Select(h => Bind(h, line)).ToArray();
				
				return new OptCallExpr(fxind, n3);
			
			//Changes
			case VariableExpr v:
				int index = currScope.get(currentFilename, line, currentImport, v.identifier, v.import, programCounter);
				return new OptVariableExpr(index);
			
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
			
			default:
				return p;
		}
	}
	
	string getRealImport(string i){
		return i == "local" ? currentImport : i == null ? null : symbols[currentImport].TryGetValue(i, out string a) ? a : i;
	}
}

record BindedScript(Snippet body, TabFunc[] functions, Allocator allocator){
	public override string ToString(){
		return body.ToString() +
			"\n" + string.Join("\n", functions.Select(f => f.ToString()));
	}
} 