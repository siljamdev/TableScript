using System;

namespace TabScript;

//Purpose: optimize expressions and statements to avoid unecessary code
class Optimizer{
	TableScript p;
	
	public Optimizer(TableScript s){
		p = s;
	}
	
	public TableScript Optimize(){
		List<Stmt> top = new(p.body.body.Length);
		
		foreach(Stmt v in p.body.body){
			Stmt bf = Optimize(v);
			if(bf != null){
				top.Add(bf);
			}
		}
		
		List<TabFunc> fs = new(p.functions.Length);
		
		foreach(TabFunc fun in p.functions){
			fs.Add(Optimize(fun));
		}
		
		return new TableScript(new Snippet(p.body.filename, p.body.import, top.ToArray()), fs.ToArray());
	}
	
	Stmt Optimize(Stmt p){
		switch(p){
			case ExprStmt e:
				Expr x = Optimize(e.exp);
				if(x is LiteralExpr){
					return null;
				}
				return new ExprStmt(x, p.line);
			
			case BlockStmt b:
				Stmt[] ne = b.inner.Select(h => Optimize(h)).Where(h => h != null).ToArray();
				return new BlockStmt(ne, p.line);
			
			case OptVarDeclStmt v:
				x = Optimize(v.val);
				return new OptVarDeclStmt(v.depth, v.index, x, p.line);
			
			case OptTabAssignStmt a:
				x = Optimize(a.val);
				return new OptTabAssignStmt(a.depth, a.index, x, p.line);
			
			case OptElementAssignStmt l:
				x = Optimize(l.val);
				IndexExpr idd2 = (IndexExpr) Optimize(l.ind);
				return new OptElementAssignStmt(l.depth, l.index, idd2, x, p.line);
			
			case IfStmt f:
				x = Optimize(f.condition);
				
				if(x is LiteralExpr lit){
					if(lit.val.Truthy){
						return Optimize(f.then);
					}else{
						return Optimize(f.els);
					}
				}
				
				return new IfStmt(x, Optimize(f.then), Optimize(f.els), p.line);
			
			case WhileStmt w:
				return new WhileStmt(Optimize(w.condition), Optimize(w.body), Optimize(w.els), p.line);
			
			case DoStmt d:
				return new DoStmt(Optimize(d.condition), Optimize(d.body), Optimize(d.els), p.line);
			
			case ForeachStmt t:
				return new ForeachStmt(t.id, Optimize(t.pool), (BlockStmt) Optimize(t.body), Optimize(t.els), p.line);
			
			case ReturnStmt r:
				return new ReturnStmt(Optimize(r.val), p.line);
			
			default:
				return p;
		}
	}
	
	TabFunc Optimize(TabFunc p){
		switch(p){
			case TabNativeFunc f:
				return new TabNativeFunc(f.import, f.identifier, f.pars, f.self, f.export, (BlockStmt) Optimize(f.body), p.filename, p.line);
			
			default:
				return p;
		}
	}
	
	Expr Optimize(Expr p){
		switch(p){
			case BinaryExpr b:
				Expr o1 = Optimize(b.left);
				Expr o2 = Optimize(b.right);
				
				if(o1 is LiteralExpr lit1 && o2 is LiteralExpr lit2){
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
					}
					
					return new BinaryExpr(o1, b.op, o2);
				}else if(b.op == TokenType.And && o1 is UnaryExpr u1 && o2 is UnaryExpr u2 && u1.op == TokenType.Exclamation && u2.op == TokenType.Exclamation){ //Morgans law
					return new UnaryExpr(TokenType.Exclamation, new BinaryExpr(u1.right, TokenType.Or, u2.right));
				}else if(b.op == TokenType.Or && o1 is UnaryExpr u3 && o2 is UnaryExpr u4 && u3.op == TokenType.Exclamation && u4.op == TokenType.Exclamation){ //Morgans law
					return new UnaryExpr(TokenType.Exclamation, new BinaryExpr(u3.right, TokenType.And, u4.right));
				}else{
					return new BinaryExpr(o1, b.op, o2);
				}
			
			case UnaryExpr u:
				o2 = Optimize(u.right);
				
				if(o2 is LiteralExpr lit2b){
					switch(u.op){
						case TokenType.Exclamation:
							return new LiteralExpr(Table.GetBool(!lit2b.val.Truthy));
						
						case TokenType.Caret:
							return new LiteralExpr(new Table(lit2b.val.AsString()));
						
						case TokenType.Percentage:
							return new LiteralExpr(new Table(lit2b.val.SplitToChars()));
					}
					
					return new UnaryExpr(u.op, o2);
				}else{
					return new UnaryExpr(u.op, o2);
				}
			
			case TernaryExpr q:
				Expr cond = Optimize(q.cond);
				Expr tru = Optimize(q.tr);
				Expr fal = Optimize(q.fa);
				
				if(cond is LiteralExpr lit1k){
					if(lit1k.val.Truthy && tru is LiteralExpr lit2k){
						return lit2k;
					}else if(!lit1k.val.Truthy && fal is LiteralExpr lit3k){
						return lit3k;
					}else{
						return new TernaryExpr(cond, tru, fal);
					}
				}else{
					return new TernaryExpr(cond, tru, fal);
				}
			
			case GroupingExpr g:
				return Optimize(g.exp);
			
			case GetElementExpr e:
				o1 = Optimize(e.left);
				IndexExpr idd2 = (IndexExpr) Optimize(e.ind);
				if(idd2.val == null && idd2.ind.mode != TabIndexMode.Random && o1 is LiteralExpr lit1b){
					return new LiteralExpr(lit1b.val.GetElem(idd2.ind));
				}else{
					return new GetElementExpr(o1, idd2);
				}
			
			case GetRangeExpr r:
				o1 = Optimize(r.left);
				IndexExpr idd = (IndexExpr) Optimize(r.ind);
				IndexExpr lld = (IndexExpr) Optimize(r.len);
				
				if(o1 is LiteralExpr lit1c){
					
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
				}
				
				return new GetRangeExpr(o1, idd, lld);
			
			case IndexExpr indxx:
				if(indxx.val == null){
					return indxx;
				}
				
				Expr o3o2 = Optimize(indxx.val);
				
				if(o3o2 is LiteralExpr jum){
					return new IndexExpr(new TabIndex(TabIndexMode.Number, jum.val.Length), null);
				}
				return new IndexExpr(default, o3o2);
			
			case BuildLiteralExpr d:
				Expr[] n = d.parts.Select(h => Optimize(h)).ToArray();
				
				if(n.All(h => h is LiteralExpr)){
					Table t = new Table();
					
					foreach(Expr xx in n){
						t.AddRange(((LiteralExpr) xx).val);
					}
					return new LiteralExpr(t);
				}else{
					List<Expr> j = new();
					
					foreach(Expr xx in n){
						if(xx is BuildLiteralExpr bbb){
							j.AddRange(bbb.parts);
						}else{
							j.Add(xx);
						}
					}
					
					return new BuildLiteralExpr(j.ToArray());
				}
			
			case DollarExpr r:
				Expr[] n2 = r.parts.Select(h => Optimize(h)).ToArray();
				
				if(!n2.All(h => h is LiteralExpr)){
					return new DollarExpr(n2);
				}
				
				string t2 = "";
				
				foreach(Expr xx in n2){
					t2 += ((LiteralExpr) xx).val.AsString();
				}
				return new LiteralExpr(new Table(t2));
			
			case OptCallExpr c:
				Expr[] n3 = c.args.Select(h => Optimize(h)).ToArray();
				
				return new OptCallExpr(c.index, n3);
			break;
			
			default:
				return p;
		}
	}
}