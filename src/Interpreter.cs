using System;

namespace TabScript;

class Interpreter{	
	List<Table> stack = new();
	Stack<int> stackPointers = new();
	int stackPointer => stackPointers.Peek();
	
	bool breakingLoop;
	bool continuingLoop;
	
	bool exiting;
	
	Table returnVal;
	
	string currentFilename;
	
	TabFunc[] functions;
	Stmt[] mainBody;
	
	internal bool interpreted = false;
	
	public Interpreter(TableScript t){		
		functions = t.functions;
		mainBody = t.body.body;
		currentFilename = t.body.filename;
	}
	
	public void Interpret(Table args){
		stackPointers.Push(0);
		assignStack(0, args.Clone()); //args ALWAYS 0
		
		foreach(Stmt s in mainBody){
			Interpret(s);
			
			if(exiting){
				break;
			}
		}
		
		interpreted = true;
	}
	
	//Only call after Interpreting
	public Table CallFunction(string import, string identifier, params Table[] functionArgs){
		if(!interpreted){
			throw new TabScriptException(TabScriptErrorType.Runtime, currentFilename, -1, "Cannot call a function before having run the script");
		}
		
		int fxIndex = Array.FindIndex(functions, f => f.Matches(import, identifier, functionArgs.Length)); //Match in available functions
		if(fxIndex == -1){
			throw new TabScriptException(TabScriptErrorType.Runtime, currentFilename, -1, "No function available with '" + (import == null ? "" : import + "::") + identifier + "' as identifier and " + functionArgs.Length + " parameters");
		}
		
		breakingLoop = false;
		continuingLoop = false;
		exiting = false;
		
		return callFunc(new OptCallExpr(fxIndex, functionArgs.Select(a => new LiteralExpr(a)).ToArray()));
	}
	
	Table getStack(int index){
		if(index < 0 || index >= stack.Count){
			return null;
		}
		
		return stack[index];
	}
	
	Table acessStack(int index){
		if(index < 0){
			return getStack(-index - 1);
		}
		
		return getStack(stackPointer + index);
	}
	
	void setStack(int index, Table t){
		if(index == stack.Count){
			stack.Add(t);
			return;
		}
		
		if(index < 0 || index >= stack.Count){
			return;
		}
		
		stack[index] = t;
	}
	
	void assignStack(int index, Table t){
		if(index < 0){
			setStack(-index - 1, t);
			return;
		}
		
		setStack(stackPointer + index, t);
	}
	
	void Interpret(Stmt s){
		switch(s){
			case ExprStmt e:
				Table t = eval(e.exp);
				//Console.WriteLine(t.AsString()); //Debug
			break;
			
			case BlockStmt b:
				foreach(Stmt g in b.inner){
					if(breakingLoop || continuingLoop || returnVal != null || exiting){
						return;
					}
					Interpret(g);
				}
			break;
			
			case OptVarAssignStmt a:
				assignStack(a.index, eval(a.val).Clone());
			break;
			
			case OptElementAssignStmt l:
				acessStack(l.index).SetElem(evalIndex(l.ind), eval(l.val));
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
			
			case OptForeachStmt ft:
				Table pool = eval(ft.pool);
				
				int iterVarIndex = ft.index;
				
				for(int i = 0; i < pool.Length; i++){
					assignStack(iterVarIndex, new Table(pool[i]));
					
					foreach(Stmt g in ft.body.inner){
						if(breakingLoop || continuingLoop || returnVal != null || exiting){
							break;
						}
						Interpret(g);
					}
					
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
				throw new TabScriptException(TabScriptErrorType.Runtime, currentFilename, s.line, "Invalid statement: " + s);
			break;
		}
	}
	
	Table callFunc(OptCallExpr x){
		TabFunc fun = functions[x.index];
		
		if(fun.arity != x.args.Length){
			throw new TabScriptException(TabScriptErrorType.Runtime, currentFilename, fun.line, "Non-matching arity in call: " + x);
		}
		
		Table[] args = x.args.Select(h => eval(h).Clone()).ToArray();
		
		if(fun is TabNativeFunc funs){			
			string tempCF = currentFilename;
			currentFilename = fun.filename;
			
			stackPointers.Push(stack.Count);
			
			for(int i = 0; i < args.Length; i++){
				assignStack(i, args[i]);
			}
			
			foreach(Stmt g in funs.body.inner){
				Interpret(g);
				
				if(returnVal != null || exiting){
					break;
				}
			}
			
			currentFilename = tempCF;
			
			stack.RemoveRange(stackPointer, stack.Count - stackPointer);
			stackPointers.Pop();
			
			Table retVal = returnVal ?? new Table(0);
			returnVal = null;
			
			return retVal;
		}else if(fun is TabExternFunc funn){
			Table ret = funn.body(args);
			return ret ?? new Table(0);
		}else{
			return new Table(0);
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
			
			case LiteralExpr l:
				return l.val;
			
			case OptVariableExpr v:
				return acessStack(v.index);
			
			case GetElementExpr e:
				return eval(e.left).GetElem(evalIndex(e.ind));
			
			case GetRangeExpr e2:
				return eval(e2.left).GetRange(evalIndex(e2.ind), evalIndex(e2.len));
			
			case BuildLiteralExpr d:
				Table t = new Table();
				foreach(Expr xx in d.parts){
					t.AddRange(eval(xx));
				}
				return t;
			
			case OptCallExpr c:
				return callFunc(c);
			
			default:
				throw new TabScriptException(TabScriptErrorType.Runtime, currentFilename, -1, "Invalid expression: " + x);
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
				throw new TabScriptException(TabScriptErrorType.Runtime, currentFilename, -1, "Invalid unary operator: " + Token.GetAsString(u.op));
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
			
			case TokenType.Star:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return ta.Product(tb);
			
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
					return Table.True;
				}else{
					tb = eval(b.right);
					
					return Table.GetBool(tb.Truthy);
				}
			
			case TokenType.At:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(tb.Contains(ta));
			
			case TokenType.ExclamationAt:
				ta = eval(b.left);
				tb = eval(b.right);
				
				return Table.GetBool(!tb.Contains(ta));
			
			default:
				throw new TabScriptException(TabScriptErrorType.Runtime, currentFilename, -1, "Invalid binary operator: " + Token.GetAsString(b.op));
				return null;
		}
	}
	
	void printStack(){
		Console.WriteLine("\n");
		for(int i = 0; i < stack.Count; i++){
			if(i == stackPointer){
				Console.Write("SP  ");
			}else{
				Console.Write("    ");
			}
			Console.WriteLine(i + ": " + stack[i]);
		}
		
		if(stackPointer == stack.Count){
			Console.WriteLine("SP");
		}
	}
}