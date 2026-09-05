using System;

namespace TableScript;

//Purpose of this class: transforming variables into unique ids, functions into indices, cutting unused functions, and producing the CFG
class Binder{
	public Action<TableScriptException> OnReport;
	public bool hadError{get; private set;}
	
	//Variables
	Allocator alloc = new Allocator();
	IScope globalScope;
	IScope currScope;
	
	string currentImport;
	string currentFilename;
	
	//break, continue, return, illegal usage outside of loop
	Stack<bool> checkingFunctions = new(new[]{false});
	bool checkingFunction  => checkingFunctions.Peek();
	
	Stack<(List<RedirectCFGNode> breaks, List<RedirectCFGNode> continues)?> loops = new Stack<(List<RedirectCFGNode> breaks, List<RedirectCFGNode> continues)?>(new (List<RedirectCFGNode> breaks, List<RedirectCFGNode> continues)?[]{null});
	List<RedirectCFGNode> breaks => loops.Peek()?.breaks;
	List<RedirectCFGNode> continues => loops.Peek()?.continues;
	
	bool checkingLoop => breaks != null && continues != null;
	
	Snippet main; //Main code
	Snippet[] bodies; //Secondary code
	
	//All functions
	TabFunc[] allFuncs;
	
	//Functions currently available
	TabFunc[] funcs;
	
	Dictionary<string, Dictionary<string, string>> symbols; //Available imports and what they are called
	
	//Functions that are used and are therefore kept
	Dictionary<int, BoundFunc> boundFuncs = new();
	Dictionary<TabFunc, int> funcsIndex = new();
	
	Optimizations opt;
	bool removeUnusedFunctions;
	
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
	
	public BoundScript Bind(){
		globalScope = new GlobalScope(alloc);
		globalScope.define(main.filename, 0, main.import, "args", false); //args variable, defined here so it has index 0
		
		CFGFragment first = null;
		CFGFragment curr = first;
		
		//Secondary bodies
		foreach(Snippet sec in bodies){
			currentFilename = sec.filename;
			updateImport(sec.import);
			currScope = new Scope(globalScope, alloc, currentImport);
			
			CFGFragment frag = BuildMany(sec.body);
			
			replaceExit(curr, frag);
			
			first ??= frag;
			curr = frag;
			
			currScope.endOfLife();
		}
		
		//Main body
		currentFilename = main.filename;
		updateImport(main.import);
		currScope = new Scope(globalScope, alloc, main.import);
		
		CFGFragment frag2 = BuildMany(main.body);
		
		replaceExit(curr, frag2);
		
		first ??= frag2;
		curr = frag2;
		
		currScope.endOfLife();
		
		//Prevent unused functions from adding new functions to funcsFinal
		BoundFunc[] funcsFinalCopy = boundFuncs.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
		
		//Not continue with errors in unused functions // keep everything if configured
		for(int i = 0; i < allFuncs.Length; i++){
			if(funcsIndex.ContainsKey(allFuncs[i])){
				continue;
			}
			
			try{
				int fxind = funcsIndex.Count;
				funcsIndex[allFuncs[i]] = fxind;
				boundFuncs[fxind] = BindFunc(allFuncs[i], fxind);
			}catch(TableScriptException e){
				hadError = true;
				OnReport?.Invoke(e);
			}
		}
		
		if(!removeUnusedFunctions){
			funcsFinalCopy = boundFuncs.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
		}
		
		if(hadError){
			throw new TableScriptException(TableScriptErrorType.Binder, main.filename, -1, "Errors present: Unable to continue");
		}else{
			return new BoundScript(main.filename, first.entry, funcsFinalCopy, alloc);
		}
	}
	
	void replaceExit(CFGFragment frag, CFGFragment exit){
		replaceExit(frag, exit?.entry);
	}
	
	void replaceExit(CFGFragment frag, CFGNode exit){
		if(frag == null){
			return;
		}
		
		if(frag.exit is StmtCFGNode s){
			s.next = exit;
		}else if(frag.exit is DummyCFGNode d){
			d.replace(exit);
		}else if(frag.exit is RedirectCFGNode){ //We cant do this
			return;
		}else{
			throw new TableScriptException(TableScriptErrorType.Binder, currentFilename, -1, "Invdalid CFG: fragment exit was not stmt nor dummy");
		}
		frag.exit = exit;
	}
	
	CFGFragment BuildMany(Stmt[] ss){
		if(ss.Length == 0){
			DummyCFGNode dummy = new();
			return new CFGFragment(dummy, dummy);
		}
	
		CFGFragment first = null;
		CFGFragment cur = first;
		
		foreach(Stmt s in ss){
			try{
				CFGFragment frag = Build(s);
				if(frag == null){
					continue;
				}
				replaceExit(cur, frag);
				
				first ??= frag;
				cur = frag;
			}catch(TableScriptException e){
				hadError = true;
				OnReport?.Invoke(e);
			}
		}
		
		return new CFGFragment(first?.entry, cur?.exit);
	}
	
	CFGFragment Build(Stmt p){
		switch(p){
			case BlockStmt b:
				currScope = new Scope(currScope, alloc, currentImport);
				
				CFGFragment bodyFrag2 = BuildMany(b.inner);
				
				currScope = currScope.endOfLife();
				
				return bodyFrag2;
			
			case IfStmt f:
				CondCFGNode condition = new CondCFGNode(BindExpr(f.condition, p.line));
				CFGNode exit = new DummyCFGNode();
				
				CFGFragment trueFrag = Build(f.then);
				CFGFragment falseFrag = Build(f.els);
				replaceExit(trueFrag, exit);
				replaceExit(falseFrag, exit);
				
				condition.isTrue = trueFrag.entry;
				condition.isFalse = falseFrag?.entry ?? exit;
				
				return new CFGFragment(condition, exit);
			
			case WhileStmt w:
				condition = new CondCFGNode(BindExpr(w.condition, w.line));
				exit = new DummyCFGNode();
				
				loops.Push((new(), new()));
				
				CFGFragment bodyFrag = Build(w.body);
				replaceExit(bodyFrag, condition);
				
				foreach(CFGNode dum in breaks){
					dum.replace(exit);
				}
				foreach(CFGNode dum in continues){
					dum.replace(condition);
				}
				loops.Pop();
				
				CFGFragment elseFrag = Build(w.els);
				replaceExit(elseFrag, exit);
				
				condition.isTrue = bodyFrag.entry;
				condition.isFalse = elseFrag?.entry ?? exit;
				
				return new CFGFragment(condition, exit);
			
			case ForeachStmt t:
				currScope = new Scope(currScope, alloc, currentImport);
				
				StmtCFGNode init = new StmtCFGNode(new Stmt[]{
					Bind(new TabDeclStmt("_pool", t.pool, p.line)),
					Bind(new TabDeclStmt("_count", new LiteralExpr(new Table(0)), p.line))
				});
				
				condition = new CondCFGNode(BindExpr(new BinaryExpr(new VariableExpr("_count", null), TokenType.Less, new VariableExpr("_pool", null)), p.line));
				exit = new DummyCFGNode();
				
				init.next = condition;
				
				StmtCFGNode getItem = new StmtCFGNode(new Stmt[]{
					Bind(new TabDeclStmt(t.id, new GetElementExpr(new VariableExpr("_pool", null), new IndexExpr(default, new VariableExpr("_count", null))), p.line))
				});
				
				StmtCFGNode end = new StmtCFGNode(new Stmt[]{
					Bind(new VarAssignStmt("_count", null, new BinaryExpr(new VariableExpr("_count", null), TokenType.Plus, new LiteralExpr(new Table(1))), p.line))
				});
				
				end.next = condition;
				
				loops.Push((new(), new()));
				
				bodyFrag = BuildMany(t.body.inner);
				replaceExit(bodyFrag, end);
				
				currScope = currScope.endOfLife();
				foreach(CFGNode dum in breaks){
					dum.replace(exit);
				}
				foreach(CFGNode dum in continues){
					dum.replace(end);
				}
				loops.Pop();
				
				getItem.next = bodyFrag.entry;
				
				elseFrag = Build(t.els);
				replaceExit(elseFrag, exit);
				
				condition.isTrue = getItem;
				condition.isFalse = elseFrag?.entry ?? exit;
				
				return new CFGFragment(init, exit);
			
			case DoStmt du:
				condition = new CondCFGNode(BindExpr(du.condition, p.line));
				exit = new DummyCFGNode();
				
				loops.Push((new(), new()));
				
				bodyFrag = Build(du.body);
				replaceExit(bodyFrag, condition);
				
				foreach(CFGNode dum in breaks){
					dum.replace(exit);
				}
				foreach(CFGNode dum in continues){
					dum.replace(condition);
				}
				loops.Pop();
				
				elseFrag = Build(du.els);
				replaceExit(elseFrag, exit);
				
				condition.isTrue = bodyFrag.entry;
				condition.isFalse = elseFrag?.entry ?? exit;
				
				return new CFGFragment(bodyFrag.entry, exit);
			
			case BreakStmt:
				if(!checkingLoop){
					throw new TableScriptException(TableScriptErrorType.Binder, currentFilename, p.line, "Break statement outside of loop");
				}
				RedirectCFGNode redir = new RedirectCFGNode();
				breaks.Add(redir);
				return new CFGFragment(redir, redir);
			
			case ContinueStmt:
				if(!checkingLoop){
					throw new TableScriptException(TableScriptErrorType.Binder, currentFilename, p.line, "Continue statement outside of loop");
				}
				redir = new RedirectCFGNode();
				continues.Add(redir);
				return new CFGFragment(redir, redir);
			
			case ReturnStmt r:
				if(!checkingFunction){
					throw new TableScriptException(TableScriptErrorType.Binder, currentFilename, p.line, "Return statement outside of function");
				}
				StmtCFGNode n2 = new StmtCFGNode(new[]{Bind(r)});
				return new CFGFragment(n2, n2);
			
			case null:
				return null;
			
			default:
				StmtCFGNode n = new StmtCFGNode(new[]{Bind(p)});
				return new CFGFragment(n, n);
		}
	}
	
	Stmt Bind(Stmt p){
		switch(p){
			//Changes
			case TabDeclStmt k:				
				Expr v = BindExpr(k.val, p.line); //First function so you cant do tab a = a;
				
				int uid = currScope.define(currentFilename, p.line, currentImport, k.identifier, false);
				
				return new BoundVarAssignStmt(uid, v, p.line);
			
			//Changes
			case GlobalDeclStmt k2:				
				v = BindExpr(k2.val, p.line);
				
				uid = globalScope.define(currentFilename, p.line, currentImport, k2.identifier, k2.export);
				
				return new BoundVarAssignStmt(uid, v, p.line);
			
			//Changes
			case VarAssignStmt a:
				v = BindExpr(a.val, p.line);
				
				uid = currScope.assign(currentFilename, p.line, currentImport, a.identifier, a.import);
				
				return new BoundVarAssignStmt(uid, v, p.line);
			
			//Changes
			case ElementAssignStmt l:
				v = BindExpr(l.val, p.line);
				
				IndexExpr idd2 = (IndexExpr) BindIndex(l.ind, p.line);
				uid = currScope.assign(currentFilename, p.line, currentImport, l.identifier, l.import);
				
				return new BoundElementAssignStmt(uid, idd2, v, p.line);
			
			case ExprStmt e:
				return new ExprStmt(BindExpr(e.exp, p.line), p.line);
			
			case ReturnStmt r:
				return new ReturnStmt(BindExpr(r.val, p.line), p.line);
			
			default:
				return p;
		}
	}
	
	//Modifies it
	BoundFunc BindFunc(TabFunc p, int index){
		switch(p){
			case TabNativeFunc f:
				if(f.pars.Length != f.pars.Distinct().Count()){
					throw new TableScriptException(TableScriptErrorType.Binder, p.filename, p.line, "Function parameters must not repeat names");
				}
				
				if(allFuncs.Any(h => !ReferenceEquals(h, f) && f.SameSignature(h))){ //Avoid same.signature functions
					throw new TableScriptException(TableScriptErrorType.Binder, p.filename, p.line, "Functions must have different signatures: '" + f.import + "::" + f.identifier + "'");
				}
				
				//Scope
				IScope tempScope = currScope;
				currScope = new FunctionScope(globalScope, alloc, f.import, index);
				
				//Function + loops
				checkingFunctions.Push(true);
				loops.Push(null);
				
				//Filename + import
				string temp2 = currentFilename;
				string temp3 = currentImport;
				currentFilename = p.filename;
				updateImport(f.import);
				
				foreach(string param in f.pars){ //define parameters
					currScope.define(currentFilename, f.line, currentImport, param, false);
				}
				
				CFGFragment bodyFragment = BuildMany(f.body.inner);
				
				//Filename + import
				currentFilename = temp2;
				updateImport(temp3);
				
				//Function + loops
				checkingFunctions.Pop();
				loops.Pop();
				
				//Scope
				currScope.endOfLife();
				currScope = tempScope;
				
				return new BoundNativeFunc(p.import, p.identifier, p.arity, bodyFragment.entry);
				break;
			
			case TabExternFunc x:
				if(x.pars.Length != x.pars.Distinct().Count()){
					throw new TableScriptException(TableScriptErrorType.Binder, p.filename, p.line, "Function parameters must not repeat names");
				}
				
				if(allFuncs.Any(h => !ReferenceEquals(h, x) && x.SameSignature(h))){
					throw new TableScriptException(TableScriptErrorType.Binder, p.filename, p.line, "Function must have different signatures: " + x.import + "::" + x.identifier);
				}
				
				return new BoundExternFunc(p.import, p.identifier, p.arity, x.body);
			default:
				throw new TableScriptException(TableScriptErrorType.Binder, p.filename, p.line, "Unknown internal function type: " + p);
		}
	}
	
	Expr BindExpr(Expr p, int line){
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
						throw new TableScriptException(TableScriptErrorType.Binder, currentFilename, line, "No function available with '" + (cimport == null ? "" : cimport + "::") + c.identifier + "' as identifier and " + c.arity + " parameters");
					}
				}
				
				if(c.self && !fx.self){
					throw new TableScriptException(TableScriptErrorType.Binder, currentFilename, line, "The function '" + fx.import + "::" + fx.identifier + "' is not a self function.");
				}
				
				//Get func index
				int fxind = getBoundFunctionIndex(fx);
				
				//Bind arguments
				Expr[] n3 = c.args.Select(h => BindExpr(h, line)).ToArray();
				
				return new BoundCallExpr(fxind, n3);
			
			//Changes
			case VariableExpr v:
				int index = currScope.get(currentFilename, line, currentImport, v.identifier, getRealImport(v.import));
				return new BoundVariableExpr(index);
			
			case BinaryExpr b:
				Expr o1 = BindExpr(b.left, line);
				Expr o2 = BindExpr(b.right, line);
				
				return new BinaryExpr(o1, b.op, o2);
			
			case UnaryExpr u:
				o1 = BindExpr(u.right, line);
				return new UnaryExpr(u.op, o1);
			
			case TernaryExpr q:
				o1 = BindExpr(q.cond, line);
				o2 = BindExpr(q.tr, line);
				Expr o3 = BindExpr(q.fa, line);
				return new TernaryExpr(o1, o2, o3);
			
			case GetElementExpr e:
				o1 = BindExpr(e.left, line);
				IndexExpr idd2 = BindIndex(e.ind, line);
				return new GetElementExpr(o1, idd2);
			
			case GetRangeExpr r:
				o1 = BindExpr(r.left, line);
				IndexExpr idd = BindIndex(r.ind, line);
				IndexExpr lld = BindIndex(r.len, line);
				return new GetRangeExpr(o1, idd, lld);
			
			case IndexExpr indxx:
				return BindIndex(indxx, line);
			
			case BuildLiteralExpr d:
				Expr[] n = d.parts.Select(h => BindExpr(h, line)).ToArray();
				return new BuildLiteralExpr(n);
			
			default:
				return p;
		}
	}
	
	IndexExpr BindIndex(IndexExpr i, int line){
		return i.val == null ? i : new IndexExpr(default, BindExpr(i.val, line));
	}
	
	int getBoundFunctionIndex(TabFunc fx){
		//get its index in finals
		if(funcsIndex.TryGetValue(fx, out int fxind2)){
			return fxind2;
		}else{ //Not found in finals
			int fxind = funcsIndex.Count;
			funcsIndex[fx] = fxind;
			boundFuncs[fxind] = BindFunc(fx, fxind);
			return fxind;
		}
	}
	
	string getRealImport(string i){
		return i == "local" ? currentImport : i == null ? null : symbols[currentImport].TryGetValue(i, out string a) ? a : i;
	}
}

record BoundScript(string filename, CFGNode body, BoundFunc[] functions, Allocator allocator){
	public override string ToString(){
		return CFGNode.ToString(body) +
			"\n" + string.Join("\n", functions.Select((f, i) => "func_" + i + " " + f.ToString()));
	}
} 