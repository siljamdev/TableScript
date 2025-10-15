using System;

namespace TabScript;

class Interpreter{
	
	TabFunc[] functions;
	
	Stack<List<Table>> scopes = new();
	
	List<Table> currentScope => scopes.Peek();
	
	List<Table> globals;
	
	bool breakingLoop;
	bool continuingLoop;
	
	bool exiting;
	
	Table returnVal;
	
	public Interpreter(){
		globals = new List<Table>();
		globals.Add(new Table()); //Args
	}
	
	public void Interpret(TableScript t, Table args){
		functions = t.functions;
		
		scopes.Clear();
		scopes.Push(globals);
		
		globals[0] = args.Clone();
		
		Stmt[] mainBody = t.topLevel;
		
		foreach(Stmt s in mainBody){
			Interpret(s);
			
			if(exiting){
				break;
			}
		}
	}
	
	public void Clear(){
		globals = new List<Table>();
		globals.Add(new Table()); //Args
	}
	
	void Interpret(Stmt s){
		switch(s){
			case ExprStmt e:
				Table t = eval(e.exp);
				//Console.WriteLine(t.AsString()); //Debug
			break;
			
			case BlockStmt b:
				scopes.Push(new List<Table>());
				
				foreach(Stmt g in b.inner){
					if(breakingLoop || continuingLoop || returnVal != null || exiting){
						scopes.Pop();
						return;
					}
					Interpret(g);
				}
				
				scopes.Pop();
			break;
			
			case OptVarDeclStmt v:
				currentScope.Add(eval(v.val).Clone());
			break;
			
			case OptTabAssignStmt a:
				scopes.ElementAt(a.depth)[a.index] = eval(a.val).Clone();
			break;
			
			case OptElementAssignStmt l:
				scopes.ElementAt(l.depth)[l.index].SetElem(l.ind, eval(l.val));
			break;
			
			case IfStmt f:
				if(eval(f.condition).Truthy){
					Interpret(f.then);
				}else if(f.els != null){
					Interpret(f.els);
				}
			break;
			
			case WhileStmt w:
				while(eval(w.condition).Truthy){
					Interpret(w.body);
					
					if(returnVal != null || exiting){
						return;
					}
					
					if(breakingLoop){
						breakingLoop = false;
						return;
					}
					
					if(continuingLoop){
						continuingLoop = false;
					}
				}
				
				if(w.els != null){
					Interpret(w.els);
				}
			break;
			
			case DoStmt d:
				do{
					Interpret(d.body);
					
					if(returnVal != null || exiting){
						return;
					}
					
					if(breakingLoop){
						breakingLoop = false;
						return;
					}
					
					if(continuingLoop){
						continuingLoop = false;
					}
				}while(eval(d.condition).Truthy);
				
				if(d.els != null){
					Interpret(d.els);
				}
			break;
			
			case ForeachStmt ft:
				Table pool = eval(ft.pool);
				
				scopes.Push(new List<Table>());
				currentScope.Add(new Table(0));
				
				for(int i = 0; i < pool.Length; i++){
					currentScope[0] = new Table(pool[i]);
					
					foreach(Stmt g in ft.body.inner){
						if(breakingLoop || continuingLoop || returnVal != null || exiting){
							scopes.Pop();
							break;
						}
						Interpret(g);
					}
					
					if(returnVal != null || exiting){
						scopes.Pop();
						return;
					}
					
					if(breakingLoop){
						breakingLoop = false;
						scopes.Pop();
						return;
					}
					
					if(continuingLoop){
						continuingLoop = false;
					}
				}
				
				scopes.Pop();
				
				if(ft.els != null){
					Interpret(ft.els);
				}
			break;
			
			case BreakStmt:
				breakingLoop = true;
			break;
			
			case ContinueStmt:
				continuingLoop = true;
			break;
			
			case ReturnStmt r:
				returnVal = eval(r.val);
			break;
			
			case ExitStmt:
				exiting = true;
			break;
			
			default:
				throw new TabScriptException(TabScriptErrorType.Runtime, s.line, "Invalid statement: " + s);
			break;
		}
	}
	
	Table callFunc(OptCallExpr x){
		TabFunc fun = functions[x.index];
		
		if(fun.arity != x.args.Length){
			throw new TabScriptException(TabScriptErrorType.Runtime, fun.line, "Non-matching arity");
		}
		
		Table[] args = x.args.Select(h => eval(h).Clone()).ToArray();
		
		if(fun is TabNativeFunc funs){
			Stack<List<Table>> temp = scopes;
			scopes = new Stack<List<Table>>();
			scopes.Push(globals);
			scopes.Push(new List<Table>());
			
			for(int i = 0; i < fun.arity; i++){
				currentScope.AddRange(args);
			}
			
			foreach(Stmt g in funs.body.inner){
				Interpret(g);
				
				if(returnVal != null || exiting){
					break;
				}
			}
			
			scopes = temp;
			
			Table retVal = returnVal ?? new Table(0);
			returnVal = null;
			
			return retVal;
		}else if(fun is TabExternFunc funn){
			return funn.body(args);
		}else{
			return new Table();
		}
	}
	
	Table eval(Expr x){
		switch(x){
			case BinaryExpr b:
				return evalBin(b);
			
			case UnaryExpr u:
				return evalUn(u);
			
			case TernaryExpr q:
				if(eval(q.cond).Truthy){
					return eval(q.tr);
				}else{
					return eval(q.fa);
				}
			
			case GroupingExpr g:
				return eval(g.exp);
			
			case LiteralExpr l:
				return l.val;
			
			case OptVariableExpr v:
				return scopes.ElementAt(v.depth)[v.index];
			
			case GetElementExpr e:
				return eval(e.left).GetElem(e.ind);
			
			case GetRangeExpr e:
				return eval(e.left).GetRange(evalIndex(e.ind), evalIndex(e.len));
			
			case BuildLiteralExpr d:
				Table t = new Table();
				foreach(Expr xx in d.parts){
					t.AddRange(eval(xx));
				}
				return t;
			
			case DollarExpr l:
				Table t2 = new Table("");
				foreach(Expr xx in l.parts){
					t2[0] += eval(xx).AsString();
				}
				return t2;
			
			case OptCallExpr c:
				return callFunc(c);
			
			default:
				throw new TabScriptException(TabScriptErrorType.Runtime, -1, "Invalid expression: " + x);
				return null;
		}
	}
	
	TabIndex evalIndex(IndexExpr x){
		return x.val == null ? x.ind : new TabIndex(TabIndexMode.Number, eval(x.val).Length);
	}
	
	Table evalUn(UnaryExpr u){
		switch(u.op){
			case TokenType.Exclamation:
				return Table.GetBool(!eval(u.right).Truthy);
			
			case TokenType.Caret:
				return new Table(eval(u.right).AsString());
			
			case TokenType.Percentage:
				return new Table(eval(u.right).SplitToChars());
			
			default:
				throw new TabScriptException(TabScriptErrorType.Runtime, -1, "Invalid unary operator: " + Token.GetAsString(u.op));
				return null;
		}
	}
	
	Table evalBin(BinaryExpr b){
		Table ta, tb;
		
		switch(b.op){
			case TokenType.Plus:
				ta = eval(b.left);
				tb = eval(b.right);
				
				ta = ta.Clone();
				ta.AddRange(tb);
				
				return ta;
			
			case TokenType.Minus:
				ta = eval(b.left);
				tb = eval(b.right);
				
				ta = ta.Clone();
				ta.RemoveSingle(tb);
				
				return ta;
			
			case TokenType.DobEqual:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(ta.EqualTo(tb));
			
			case TokenType.ExclamationEqual:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(!ta.EqualTo(tb));
			
			case TokenType.Greater:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(ta.Length > tb.Length);
			
			case TokenType.GreaterEqual:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(ta.Length >= tb.Length);
			
			case TokenType.Less:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(ta.Length < tb.Length);
			
			case TokenType.LessEqual:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(ta.Length <= tb.Length);
			
			case TokenType.And:
				ta = eval(b.left);
				
				if(ta.Truthy){
					tb = eval(b.right);
					
					return Table.GetBool(tb.Truthy);
				}else{
					return Table.False;
				}
			
			case TokenType.Or:
				ta = eval(b.left);
				
				if(ta.Truthy){
					tb = eval(b.right);
					
					return Table.GetBool(tb.Truthy);
				}else{
					return Table.True;
				}
			
			case TokenType.At:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(tb.Contains(ta));
			
			default:
				throw new TabScriptException(TabScriptErrorType.Runtime, -1, "Invalid binary operator: " + Token.GetAsString(b.op));
				return null;
		}
	}
}