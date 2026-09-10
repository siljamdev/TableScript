using System;

namespace TableScript;

//Purpose: optimize expressions and statements to avoid unecessary code
class Optimizer{
	BoundScript p;
	ResolvedImport rim;
	Allocator alloc;
	
	Dictionary<CFGNode, Dictionary<int, Table>> inVars = new();
	Dictionary<int, Table> currentVariables;
	
	Dictionary<CFGNode, HashSet<int>> outLive = new();
	HashSet<int> currentInLive; //For liveness analysis
	HashSet<int> currentOutLive; //For DSE
	
	List<int> usedFuncsIndexTranslation = new();
	
	Optimizations opt;
	
	bool constFolding; //Simplify literals
	bool constBranching; //Simplify statements
	bool constPropagation; //Replace known variable values
	bool exprSimplifier; //Simplify expressions
	bool deadCodeDel; //Delete everything after exit ir return
	bool deadStoreDel; //Delete useless var writes
	bool deadFuncDel; //Delete unused funcs
	
	bool anyChanged;
	
	public Optimizer(BoundScript s, Optimizations opt){
		p = s;
		alloc = s.allocator;
		
		this.opt = opt;
		
		constFolding = (opt & Optimizations.ConstantFolding) != 0;
		constBranching = (opt & Optimizations.ConstantBranching) != 0;
		exprSimplifier = (opt & Optimizations.ExpressionSimplification) != 0;
		deadCodeDel = (opt & Optimizations.DeadCodeElimination) != 0;
		constPropagation = (opt & Optimizations.ConstantPropagation) != 0;
		deadStoreDel = (opt & Optimizations.DeadStoreElimination) != 0;
		deadFuncDel = (opt & Optimizations.DeadFunctionElimination) != 0;
	}
	
	public Script Optimize(){		
		CFGNode main = OptimizeCFG(p.body);
		
		foreach(BoundFunc fun in p.functions){
			OptimizeFunc(fun);
		}
		
		if(deadFuncDel){
			int lastLen = 0;
			walkNode(main, none, none, deleteDeadFunctions);
			
			List<BoundFunc> newFuncs = new();
			
			while(usedFuncsIndexTranslation.Count > lastLen){
				int nl = lastLen;
				lastLen = usedFuncsIndexTranslation.Count;
				
				for(int i = nl; i < lastLen; i++){
					if(p.functions[usedFuncsIndexTranslation[i]] is BoundNativeFunc n){
						walkNode(n.body, none, none, deleteDeadFunctions);
					}
				}
			}
			
			return new Script(p.filename, main, usedFuncsIndexTranslation.Select(i => p.functions[i]).ToArray());
		}else{
			return new Script(p.filename, main, p.functions);
		}
	}
	
	CFGNode OptimizeCFG(CFGNode n){
		if(n is DummyCFGNode){
			return null;
		}
		
		n.isEntry = true;
		
		anyChanged = true;
		while(anyChanged){
			anyChanged = false;
			
			walkNode(n, simplifyGraph, none, none);
			
			if(exprSimplifier){
				walkNode(n, none, simplifyExpr, simplifyExpr);
			}
			
			if(constPropagation){
				prepareInVars(n);
				walkNodeBefore(n, propagateConstantsBefore, propagateConstants, propagateConstants);
			}
			
			if(constFolding){
				walkNode(n, none, none, foldConstants);
			}
			
			if(deadStoreDel){
				analizeLiveness(n);
				walkNodeBeforeBeforeReversed(n, eliminateDeadStoreBefore, eliminateDeadStore, eliminateDeadStore);
			}
			
			if(deadCodeDel){
				walkNode(n, deleteDeadCode, none, none);
			}
			
			if(constBranching){
				walkNode(n, branchConstant, none, none);
			}
			
			//Console.WriteLine("ROUND FINISHED\n" + CFGNode.ToString(n));
		}
		
		//Variable index, only once
		walkNode(n, none, markUsedVars, markUsedVars);
		analizeLiveness(n);
		alloc.startIndexing(opt);
		walkNode(n, none, replaceVariableUids, replaceVariableUids);
		
		//Console.WriteLine("GRAPH FINISHED\n" + CFGNode.ToString(n));
		
		return n;
	}
	
	void OptimizeFunc(BoundFunc p){
		if(p is BoundNativeFunc f){
			f.body = OptimizeCFG(f.body);
		}
	}
	
	//No transformation
	void none(CFGNode n){}
	Stmt[] none(Stmt s) => new Stmt[]{s};
	Expr none(Expr e) => e;
	
	//Simplify expressions
	Stmt[] simplifyExpr(Stmt s){
		Stmt n = simplifyExprSimple(s);
		return n == null ? Array.Empty<Stmt>() : new Stmt[]{n};
	}
	public static Stmt simplifyExprSimple(Stmt s){
		if(s is ExprStmt e && !e.exp.hasSideEffects()){
			return null;
		}
		return s;
	}
	public static Expr simplifyExpr(Expr e){
		if(e is BinaryExpr b){
			Expr o1 = b.left;
			Expr o2 = b.right;
			
			//Morgans law
			if(b.op == TokenType.And && o1 is UnaryExpr u1 && o2 is UnaryExpr u2 && u1.op == TokenType.Exclamation && u2.op == TokenType.Exclamation){
				return new UnaryExpr(TokenType.Exclamation, new BinaryExpr(u1.right, TokenType.Or, u2.right));
			//Morgans law
			}else if(b.op == TokenType.Or && o1 is UnaryExpr u3 && o2 is UnaryExpr u4 && u3.op == TokenType.Exclamation && u4.op == TokenType.Exclamation){
				return new UnaryExpr(TokenType.Exclamation, new BinaryExpr(u3.right, TokenType.And, u4.right));
			
			//And simplification
			}else if(b.op == TokenType.And && o1 is LiteralExpr lftx && !lftx.val.Truthy){
				return new LiteralExpr(Table.GetBool(false));
			//Or simplification
			}else if(b.op == TokenType.Or && o1 is LiteralExpr lftx2 && lftx2.val.Truthy){
				return new LiteralExpr(Table.GetBool(true));
			
			//a.length > b.length -> a > b
			}else if(b.op == TokenType.Greater || b.op == TokenType.GreaterEqual || b.op == TokenType.Less || b.op == TokenType.LessEqual){
				if(o1 is GetElementExpr gex1 && gex1.ind.val == null && gex1.ind.ind.mode == TabIndexMode.Length){
					o1 = gex1.left;
				}
				if(o2 is GetElementExpr gex2 && gex2.ind.val == null && gex2.ind.ind.mode == TabIndexMode.Length){
					o2 = gex2.left;
				}
				
				return new BinaryExpr(o1, b.op, o2);
			}
			
			//a == 0 -> !a
			else if(b.op == TokenType.DobEqual && o2 is LiteralExpr lit7 && lit7.val.Length == 0){
				return new UnaryExpr(TokenType.Exclamation, o1);
			}
			
			//a.length == 0 -> !a
			else if(b.op == TokenType.DobEqual && o1 is GetElementExpr gee1 && gee1.ind.val == null && gee1.ind.ind.mode == TabIndexMode.Length && o2 is LiteralExpr lit777 && lit777.val.Length == 0){
				return new UnaryExpr(TokenType.Exclamation, gee1.left);
			}
		}else if(e is UnaryExpr u){
			Expr o2 = u.right;
			
			if(u.op == TokenType.Exclamation && o2 is BinaryExpr bexx){ // !(a == b) -> a != b
				switch(bexx.op){
					case TokenType.DobEqual:
						return new BinaryExpr(bexx.left, TokenType.ExclamationEqual, bexx.right);
					
					case TokenType.At:
						return new BinaryExpr(bexx.left, TokenType.ExclamationAt, bexx.right);
					
					case TokenType.Greater:
						return new BinaryExpr(bexx.left, TokenType.LessEqual, bexx.right);
					
					case TokenType.GreaterEqual:
						return new BinaryExpr(bexx.left, TokenType.Less, bexx.right);
					
					case TokenType.Less:
						return new BinaryExpr(bexx.left, TokenType.GreaterEqual, bexx.right);
					
					case TokenType.LessEqual:
						return new BinaryExpr(bexx.left, TokenType.Greater, bexx.right);
				}
			}else if(u.op == TokenType.Exclamation && o2 is UnaryExpr uuu1 && uuu1.op == TokenType.Exclamation && uuu1.right is UnaryExpr uuu2 && uuu2.op == TokenType.Exclamation){
				return uuu2; // !!!a -> !a
			}
			
			// !(a.length) -> !a
			else if(u.op == TokenType.Exclamation && o2 is GetElementExpr gee1 && gee1.ind.val == null && gee1.ind.ind.mode == TabIndexMode.Length){
				return new UnaryExpr(TokenType.Exclamation, gee1.left);
			}
		}else if(e is GetElementExpr g){
			Expr o1 = g.left;
			IndexExpr idd2 = g.ind;
			
			//a.length.length -> a.length
			if(idd2.val == null && idd2.ind.mode == TabIndexMode.Length && o1 is GetElementExpr innerE && innerE.ind.val == null && innerE.ind.ind.mode == TabIndexMode.Length){
				return innerE;
			}
		}else if(e is BuildLiteralExpr d){
			if(d.parts.Length == 0){
				return new LiteralExpr(new Table(0));
			}else if(d.parts.Length == 1){
				return d.parts[0];
			}
		}
		
		return e;
	}
	
	//Constant folding
	public static Expr foldConstants(Expr e){
		if(e is BinaryExpr b && b.left is LiteralExpr lit1 && b.right is LiteralExpr lit2){
			switch(b.op){
				case TokenType.Plus:
					Table t3 = new Table(lit1.val);
					t3.AddRange(lit2.val);
					return new LiteralExpr(t3);
				
				case TokenType.Minus:
					t3 = new Table(lit1.val);
					t3.RemoveSingle(lit2.val);
					return new LiteralExpr(t3);
				
				case TokenType.Star:
					return new LiteralExpr(lit1.val.Product(lit2.val));
				
				case TokenType.DobEqual:
					return new LiteralExpr(Table.GetBool(lit1.val.EqualTo(lit2.val)));
				
				case TokenType.ExclamationEqual:
					return new LiteralExpr(Table.GetBool(!lit1.val.EqualTo(lit2.val)));
				
				case TokenType.Greater:
					return new LiteralExpr(Table.GetBool(lit1.val.Length > lit2.val.Length));
				
				case TokenType.GreaterEqual:
					return new LiteralExpr(Table.GetBool(lit1.val.Length >= lit2.val.Length));
				
				case TokenType.Less:
					return new LiteralExpr(Table.GetBool(lit1.val.Length < lit2.val.Length));
				
				case TokenType.LessEqual:
					return new LiteralExpr(Table.GetBool(lit1.val.Length <= lit2.val.Length));
				
				case TokenType.And:
					return new LiteralExpr(Table.GetBool(lit1.val.Truthy && lit2.val.Truthy));
				
				case TokenType.Or:
					return new LiteralExpr(Table.GetBool(lit1.val.Truthy || lit2.val.Truthy));
				
				case TokenType.At:
					return new LiteralExpr(Table.GetBool(lit2.val.Contains(lit1.val)));
				
				case TokenType.ExclamationAt:
					return new LiteralExpr(Table.GetBool(!lit2.val.Contains(lit1.val)));
			}
		}else if(e is BinaryExpr b2 && b2.op == TokenType.Plus && (b2.left is BinaryExpr ba1 && ba1.op == TokenType.Plus || b2.right is BinaryExpr ba2 && ba2.op == TokenType.Plus)){
			List<Expr> summands = new();
			if(b2.left is BinaryExpr b31 && b31.op == TokenType.Plus){
				summands.Add(b31.left);
				summands.Add(b31.right);
			}else{
				summands.Add(b2.left);
			}
			
			if(b2.right is BinaryExpr b32 && b32.op == TokenType.Plus){
				summands.Add(b32.left);
				summands.Add(b32.right);
			}else{
				summands.Add(b2.right);
			}
			
			Expr final = null;
			Table current = null;
			
			foreach(Expr e33 in summands){
				if(e33 is LiteralExpr l){
					if(current == null){
						current = l.val;
					}else{
						current.AddRange(l.val);
					}
				}else{
					if(current != null){
						if(final == null){
							final = new LiteralExpr(current);
						}else{
							final = new BinaryExpr(final, TokenType.Plus, new LiteralExpr(current));
						}
						current = null;
					}
					if(final == null){
						final = e33;
					}else{
						final = new BinaryExpr(final, TokenType.Plus, e33);
					}
				}
			}
			
			if(current != null){
				final = new BinaryExpr(final, TokenType.Plus, new LiteralExpr(current));
			}
			
			return final;
		}else if(e is BinaryExpr bin && bin.op == TokenType.Star && (bin.left is BinaryExpr ba11 && ba11.op == TokenType.Star || bin.right is BinaryExpr ba21 && ba21.op == TokenType.Star)){
			List<Expr> parts = new();
			if(bin.left is BinaryExpr b31 && b31.op == TokenType.Star){
				parts.Add(b31.left);
				parts.Add(b31.right);
			}else{
				parts.Add(bin.left);
			}
			
			if(bin.right is BinaryExpr b32 && b32.op == TokenType.Star){
				parts.Add(b32.left);
				parts.Add(b32.right);
			}else{
				parts.Add(bin.right);
			}
			
			Expr final = null;
			Table current = null;
			
			foreach(Expr e33 in parts){
				if(e33 is LiteralExpr l){
					if(current == null){
						current = l.val;
					}else{
						current = current.Product(l.val);
					}
				}else{
					if(current != null){
						if(final == null){
							final = new LiteralExpr(current);
						}else{
							final = new BinaryExpr(final, TokenType.Star, new LiteralExpr(current));
						}
						current = null;
					}
					if(final == null){
						final = e33;
					}else{
						final = new BinaryExpr(final, TokenType.Star, e33);
					}
				}
			}
			
			if(current != null){
				final = new BinaryExpr(final, TokenType.Star, new LiteralExpr(current));
			}
			
			return final;
		}else if(e is UnaryExpr u && u.right is LiteralExpr lit2b){
			switch(u.op){
				case TokenType.Exclamation:
					return new LiteralExpr(Table.GetBool(!lit2b.val.Truthy));
				
				case TokenType.Caret:
					return new LiteralExpr(new Table(lit2b.val.AsString()));
				
				case TokenType.Percentage:
					return new LiteralExpr(new Table(lit2b.val.SplitToChars()));
			}
		}else if(e is TernaryExpr t && t.cond is LiteralExpr lit1k){
			if(lit1k.val.Truthy){
				return t.tr;
			}else{
				return t.fa;
			}
		}else if(e is GetElementExpr g && g.left is LiteralExpr lit1b){
			IndexExpr idd2 = g.ind;
			if(idd2.val == null && idd2.ind.mode != TabIndexMode.Random){ //literal[smth not random not expr] -> literal
				return new LiteralExpr(lit1b.val.GetElem(idd2.ind));
			}
		}else if(e is GetRangeExpr r && r.left is LiteralExpr lit1c){
			IndexExpr idd = r.ind;
			IndexExpr lld = r.len;
			
			TabIndex? p1 = null;
			TabIndex? p2 = null;
			
			if(idd.val == null && idd.ind.mode != TabIndexMode.Random){
				p1 = idd.ind;
			}
			
			if(lld.val == null){
				p2 = lld.ind;
			}
			
			if(p1 != null && p2 != null){
				return new LiteralExpr(lit1c.val.GetRange((TabIndex) p1, (TabIndex) p2));
			}
		}else if(e is BuildLiteralExpr d && d.parts.All(h => h is LiteralExpr)){
			Table t2 = new Table();
			
			foreach(Expr xx in d.parts){
				t2.AddRange(((LiteralExpr) xx).val);
			}
			return new LiteralExpr(t2);
		}
		
		return e;
	}
	
	//Constant branching
	void branchConstant(CFGNode n){
		if(n is CondCFGNode c && c.condition is LiteralExpr lit){
			if(lit.val.Truthy){
				anyChanged = c.replace(c.isTrue);
			}else{
				anyChanged = c.replace(c.isFalse);
			}
		}
	}
	
	//Dead code delete
	void deleteDeadCode(CFGNode n){
		if(n is StmtCFGNode s){
			List<Stmt> c = new(s.statements.Length);
			foreach(Stmt t in s.statements){
				c.Add(t);
				if((t is ReturnStmt || t is ExitStmt) && c.Count != s.statements.Length){
					s.statements = c.ToArray();
					s.next = null;
					anyChanged = true;
					return;
				}
			}
		}
	}
	
	//Graph simplification
	void simplifyGraph(CFGNode n){
		if(n is StmtCFGNode s){
			List<Stmt> all = new(s.statements);
			
			CFGNode current = s.next;
			while(current is StmtCFGNode x && x.entries.Count == 1){
				all.AddRange(x.statements);
				
				current = x.next;
				x.replace(x.next);
				
				anyChanged = true;
			}
			
			if(all.Count == 0){
				s.replace(current);
				
				anyChanged = true;
			}else{
				s.statements = all.ToArray();
			}
		}else if(n is CondCFGNode c){
			if(c.isTrue == c.isFalse && c.isTrue != null){
				StmtCFGNode rep = new(new ExprStmt(c.condition, -1));
				rep.next = c.isTrue;
				c.replace(rep);
				
				anyChanged = true;
			}
		}else{
			n.replace(null); //Dummy
		}
	}
	
	//Constant propagation
	void propagateConstantsBefore(CFGNode n){
		//Set the dict so it can be accessed
		currentVariables = inVars[n];
	}
	Stmt[] propagateConstants(Stmt s){
		if(s is BoundVarAssignStmt a){
			if(a.val is LiteralExpr lit){
				currentVariables[a.index] = lit.val;
			}else{
				currentVariables[a.index] = null;
			}
		}else if(s is BoundElementAssignStmt e){
			if(e.val is LiteralExpr lit && e.ind.val == null && e.ind.ind.mode != TabIndexMode.Random){
				Table t = currentVariables[e.index].Clone();
				t.SetElem(e.ind.ind, lit.val);
				currentVariables[e.index] = t;
			}else{
				currentVariables[e.index] = null;
			}
		}
		
		return new Stmt[]{s};
	}
	Expr propagateConstants(Expr e){
		if(e is BoundVariableExpr v && currentVariables.TryGetValue(v.index, out Table val) && val != null){
			return new LiteralExpr(val);
		}
		return e;
	}
	
	//Populate inVars dict. Its a dictionary of the know variables values at the start of a node
	void prepareInVars(CFGNode entry){
		inVars.Clear();
		Dictionary<CFGNode, Dictionary<int, Table>> outVars = new();
		
		Queue<CFGNode> toProcess = new();
		HashSet<CFGNode> queued = new();
		
		toProcess.Enqueue(entry);
		queued.Add(entry);
		
		while(toProcess.Count > 0){
			CFGNode cur = toProcess.Dequeue();
			queued.Remove(cur);
			
			Dictionary<int, Table> input = null;
			
			foreach(CFGNode e in cur.entries){
				if(!outVars.TryGetValue(e, out Dictionary<int, Table> outE)){
					continue;
				}
				
				if(input == null){
					input = new(outE);
				}else{
					input = meet(input, outE);
				}
			}
			
			input ??= new();
			
			inVars[cur] = input;
			
			Dictionary<int, Table> output = new(input);
			
			insideVars(cur, output);
			
			bool changed = !outVars.TryGetValue(cur, out Dictionary<int, Table> old) || !dictEqual(old, output);
			
			outVars[cur] = output;
			
			if(changed){
				foreach(CFGNode suc in cur.successors()){
					if(queued.Add(suc)){
						toProcess.Enqueue(suc);
					}
				}
			}
		}
	}
	
	//Add to the output the variable assignments of the node
	void insideVars(CFGNode n, Dictionary<int, Table> output){
		if(n is StmtCFGNode s){
			foreach(Stmt t in s.statements){
				if(t is BoundVarAssignStmt b){
					if(b.val is LiteralExpr lit){
						output[b.index] = lit.val;
					}else{
						output[b.index] = null;
					}
				}else if(t is BoundElementAssignStmt e){
					if(e.val is LiteralExpr lit && e.ind.val == null && e.ind.ind.mode != TabIndexMode.Random){
						Table t22 = output[e.index].Clone();
						t22.SetElem(e.ind.ind, lit.val);
						output[e.index] = t22;
					}else{
						output[e.index] = null;
					}
				} 
			}
		}
	}
	
	//Combine dicts of entry nodes
	Dictionary<int, Table> meet(Dictionary<int, Table> a, Dictionary<int, Table> b){
		Dictionary<int, Table> res = new();
		
		foreach(int key in a.Keys.Union(b.Keys)){
			bool hasA = a.TryGetValue(key, out Table? t1);
			bool hasB = b.TryGetValue(key, out Table? t2);
			
			if(!hasA || !hasB){
				continue;
			}
			
			if(t1 == null || t2 == null){
				res[key] = null;
				continue;
			}
			
			res[key] = t1.EqualTo(t2) ? t1 : null;
		}
		
		return res;
	}
	
	bool dictEqual(Dictionary<int, Table> a, Dictionary<int, Table> b){
		if(a.Count != b.Count){
			return false;
		}
		
		foreach(KeyValuePair<int, Table> kvp in a){
			if(!b.TryGetValue(kvp.Key, out Table tb)){
				return false;
			}
			
			if(kvp.Value == null && tb == null){
				continue;
			}
			
			if(kvp.Value == null && tb != null){
				return false;
			}
			
			if(!kvp.Value.EqualTo(tb)){
				return false;
			}
		}
		
		return true;
	}
	
	//set variable indexes
	Stmt[] replaceVariableUids(Stmt s){
		if(s is BoundVarAssignStmt a){
			int index = alloc.getIndex(a.index);
			return new Stmt[]{new BoundVarAssignStmt(index, a.val, s.line)};
		}else if(s is BoundElementAssignStmt e){
			int index = alloc.getIndex(e.index);
			return new Stmt[]{new BoundElementAssignStmt(index, e.ind, e.val, s.line)};
		}
		return new Stmt[]{s};
	}
	Expr replaceVariableUids(Expr e){
		if(e is BoundVariableExpr v){
			int index = alloc.getIndex(v.index);
			return new BoundVariableExpr(index);
		}
		return e;
	}
	
	//Dead store elimination
	void eliminateDeadStoreBefore(CFGNode n){
		//Set the set so it can be accessed
		currentOutLive = outLive[n];
	}
	Stmt[] eliminateDeadStore(Stmt s){
		if(s is BoundVarAssignStmt b){
			if(!currentOutLive.Remove(b.index) && !alloc.variables[b.index].isGlobal){
				return new Stmt[]{new ExprStmt(b.val, s.line)};
			}
		}else if(s is BoundElementAssignStmt e){
			if(!currentOutLive.Contains(e.index) && !alloc.variables[e.index].isGlobal){
				if(e.ind.val != null){
					return new Stmt[]{new ExprStmt(e.val, s.line), new ExprStmt(e.ind.val, s.line)};
				}else{
					return new Stmt[]{new ExprStmt(e.val, s.line)};
				}
			}
			currentOutLive.Add(e.index);
		}
		
		return new Stmt[]{s};
	}
	Expr eliminateDeadStore(Expr e){
		if(e is BoundVariableExpr v){
			currentOutLive.Add(v.index);
		}
		return e;
	}
	
	//Populate outLive dict. Its a dictionary of the live variables at the end of each node
	void analizeLiveness(CFGNode entry){
		outLive.Clear();
		Dictionary<CFGNode, HashSet<int>> inLive = new();
		
		Queue<CFGNode> toProcess = new();
		HashSet<CFGNode> queued = new();
		
		toProcess.Enqueue(entry);
		queued.Add(entry);
		
		while(toProcess.Count > 0){
			CFGNode cur = toProcess.Dequeue();
			queued.Remove(cur);
			
			HashSet<int> output = new();
			
			foreach(CFGNode suc in cur.successors()){
				if(inLive.TryGetValue(suc, out HashSet<int> sucInLive)){
					output.UnionWith(sucInLive);
				}
			}
			
			outLive[cur] = output;
			
			HashSet<int> input = new(output);
			
			insideLiveness(cur, input);
			
			bool first = !inLive.TryGetValue(cur, out HashSet<int> old);
			bool changed = first || !old.SetEquals(input);
			
			inLive[cur] = input;
			
			if(first){
				foreach(CFGNode suc in cur.successors()){
					if(queued.Add(suc)){
						toProcess.Enqueue(suc);
					}
				}
			}
			
			if(changed){
				foreach(CFGNode e in cur.entries){
					if(queued.Add(e)){
						toProcess.Enqueue(e);
					}
				}
			}
		}
	}
	
	void insideLiveness(CFGNode n, HashSet<int> inLive){
		currentInLive = inLive;
		if(n is StmtCFGNode s){
			foreach(Stmt t in s.statements.Reverse()){
				insideLiveness(t);
			}
		}else if(n is CondCFGNode c){
			transformExpr(c.condition, insideLiveness);
		}
		currentInLive = null;
	}
	
	//Needed because lvalue must be evaluated before rvalue expr
	void insideLiveness(Stmt s){
		if(s is BoundVarAssignStmt b){
			alloc.variables[b.index].liveness = new(currentInLive);
			currentInLive.Remove(b.index);
		}else if(s is BoundElementAssignStmt e){
			alloc.variables[e.index].liveness = new(currentInLive);
			currentInLive.Add(e.index); //Treat as a read
		}
		
		transformStmt(s, none, insideLiveness); //rvalues
	}
	Expr insideLiveness(Expr e){
		if(e is BoundVariableExpr v){
			//alloc.variables[v.index].liveness = new(currentInLive);
			currentInLive.Add(v.index);
		}
		
		return e;
	}
	
	//Dead func del
	Expr deleteDeadFunctions(Expr e){
		if(e is BoundCallExpr c){
			int sto = usedFuncsIndexTranslation.IndexOf(c.index);
			if(sto == -1){
				sto = usedFuncsIndexTranslation.Count;
				usedFuncsIndexTranslation.Add(c.index);
			}
			return new BoundCallExpr(sto, c.args);
		}
		return e;
	}
	
	//Node function is before statements, statements are evaluated in reverse order in blocks and statements are transformed before expressions
	void walkNodeBeforeBeforeReversed(CFGNode n, Action<CFGNode> nodeFuncBefore, Func<Stmt, Stmt[]> stmtFuncBefore, Func<Expr, Expr> exprFunc, HashSet<CFGNode> visited = null){
		if(n == null){
			return;
		}
		
		visited ??= new();
		
		if(!visited.Add(n)){
			return;
		}
		
		nodeFuncBefore(n);
		
		switch(n){
			case StmtCFGNode s:
				s.statements = s.statements.Reverse().SelectMany(t => transformStmtBefore(t, stmtFuncBefore, exprFunc).Reverse()).Reverse().ToArray();
				break;
			
			case CondCFGNode c:
				c.condition = transformExpr(c.condition, exprFunc);
				break;
		}
		
		switch(n){
			case StmtCFGNode s:
				walkNodeBeforeBeforeReversed(s.next, nodeFuncBefore, stmtFuncBefore, exprFunc, visited);
				break;
			
			case CondCFGNode c:
				walkNodeBeforeBeforeReversed(c.isTrue, nodeFuncBefore, stmtFuncBefore, exprFunc, visited);
				walkNodeBeforeBeforeReversed(c.isFalse, nodeFuncBefore, stmtFuncBefore, exprFunc, visited);
				break;
		}
	}
	
	//Mark variables as used
	Stmt[] markUsedVars(Stmt s){
		if(s is BoundVarAssignStmt b){
			alloc.variables[b.index].used = true;
		}else if(s is BoundElementAssignStmt e){
			alloc.variables[e.index].used = true;
		}
		
		return new Stmt[]{s};
	}
	Expr markUsedVars(Expr e){
		if(e is BoundVariableExpr v){
			alloc.variables[v.index].used = true;
		}
		return e;
	}
	
	//Node function is before statements
	void walkNodeBefore(CFGNode n, Action<CFGNode> nodeFuncBefore, Func<Stmt, Stmt[]> stmtFunc, Func<Expr, Expr> exprFunc, HashSet<CFGNode> visited = null){
		if(n == null){
			return;
		}
		
		visited ??= new();
		
		if(!visited.Add(n)){
			return;
		}
		
		nodeFuncBefore(n);
		
		switch(n){
			case StmtCFGNode s:
				s.statements = s.statements.SelectMany(t => transformStmt(t, stmtFunc, exprFunc)).ToArray();
				break;
			
			case CondCFGNode c:
				c.condition = transformExpr(c.condition, exprFunc);
				break;
		}
		
		switch(n){
			case StmtCFGNode s:
				walkNodeBefore(s.next, nodeFuncBefore, stmtFunc, exprFunc, visited);
				break;
			
			case CondCFGNode c:
				walkNodeBefore(c.isTrue, nodeFuncBefore, stmtFunc, exprFunc, visited);
				walkNodeBefore(c.isFalse, nodeFuncBefore, stmtFunc, exprFunc, visited);
				break;
		}
	}
	
	void walkNode(CFGNode n, Action<CFGNode> nodeFunc, Func<Stmt, Stmt[]> stmtFunc, Func<Expr, Expr> exprFunc, HashSet<CFGNode> visited = null){
		if(n == null){
			return;
		}
		
		visited ??= new();
		
		if(!visited.Add(n)){
			return;
		}
		
		switch(n){
			case StmtCFGNode s:
				s.statements = s.statements.SelectMany(t => transformStmt(t, stmtFunc, exprFunc)).ToArray();
				break;
			
			case CondCFGNode c:
				c.condition = transformExpr(c.condition, exprFunc);
				break;
		}
		
		nodeFunc(n);
		
		switch(n){
			case StmtCFGNode s:
				walkNode(s.next, nodeFunc, stmtFunc, exprFunc, visited);
				break;
			
			case CondCFGNode c:
				walkNode(c.isTrue, nodeFunc, stmtFunc, exprFunc, visited);
				walkNode(c.isFalse, nodeFunc, stmtFunc, exprFunc, visited);
				break;
		}
	}
	
	Stmt[] transformStmtBefore(Stmt s, Func<Stmt, Stmt[]> stmtFuncBefore, Func<Expr, Expr> exprFunc){
		Stmt[] ns = stmtFuncBefore(s);
		
		ns = ns.Select(s => s switch{
				ExprStmt e => new ExprStmt(transformExpr(e.exp, exprFunc), s.line),
				ReturnStmt r => new ReturnStmt(transformExpr(r.val, exprFunc), s.line),
				BoundVarAssignStmt a => new BoundVarAssignStmt(a.index, transformExpr(a.val, exprFunc), s.line),
				BoundElementAssignStmt m => new BoundElementAssignStmt(m.index, transformIndex(m.ind, exprFunc), transformExpr(m.val, exprFunc), s.line),
				
				_ => s
			}).ToArray();
		
		
		if(ns.Length < 1 || ns.Length > 1 || s != ns[0]){
			anyChanged = true;
		}
		
		return ns;
	}
	
	Stmt[] transformStmt(Stmt s, Func<Stmt, Stmt[]> stmtFunc, Func<Expr, Expr> exprFunc){
		s = s switch{
			ExprStmt e => new ExprStmt(transformExpr(e.exp, exprFunc), s.line),
			ReturnStmt r => new ReturnStmt(transformExpr(r.val, exprFunc), s.line),
			BoundVarAssignStmt a => new BoundVarAssignStmt(a.index, transformExpr(a.val, exprFunc), s.line),
			BoundElementAssignStmt m => new BoundElementAssignStmt(m.index, transformIndex(m.ind, exprFunc), transformExpr(m.val, exprFunc), s.line),
			
			_ => s
		};
		
		Stmt[] ns = stmtFunc(s);
		
		if(ns.Length < 1 || ns.Length > 1 || s != ns[0]){
			anyChanged = true;
		}
		
		return ns;
	}
	
	Expr transformExpr(Expr p, Func<Expr, Expr> func){
		p = p switch{
			BinaryExpr b => new BinaryExpr(transformExpr(b.left, func), b.op, transformExpr(b.right, func)),
			UnaryExpr u => new UnaryExpr(u.op, transformExpr(u.right, func)),
			TernaryExpr t => new TernaryExpr(transformExpr(t.cond, func), transformExpr(t.tr, func), transformExpr(t.fa, func)),
			GetElementExpr g => new GetElementExpr(transformExpr(g.left, func), transformIndex(g.ind, func)),
			GetRangeExpr r => new GetRangeExpr(transformExpr(r.left, func), transformIndex(r.ind, func), transformIndex(r.len, func)),
			IndexExpr i => transformIndex(i, func),
			BuildLiteralExpr l => new BuildLiteralExpr(l.parts.Select(e => transformExpr(e, func)).ToArray()),
			BoundCallExpr c => new BoundCallExpr(c.index, c.args.Select(e => transformExpr(e, func)).ToArray()),
			
			_ => p
		};
		
		Expr ne = func(p);
		
		if(p != ne){
			anyChanged = true;
		}
		
		return ne;
	}
	
	IndexExpr transformIndex(IndexExpr p, Func<Expr, Expr> func){
		if(p.val == null){
			return p;
		}
		
		Expr v = transformExpr(p.val, func);
		
		if(constFolding && v is LiteralExpr jum){
			anyChanged = true;
			return new IndexExpr(new TabIndex(TabIndexMode.Number, jum.val.Length), null);
		}
		
		return new IndexExpr(default, v);
	}
}
