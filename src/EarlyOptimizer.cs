using System;

namespace TableScript;

//Purpose: early optimizations
class EarlyOptimizer{
	ResolvedImport rim;
	
	Optimizations opt;
	
	bool constFolding; //Simplify literals
	bool constBranching; //Simplify statements
	bool exprSimplifier; //Simplify expressions
	bool deadCodeDel; //Delete code after returns or exits
	
	bool anyChanged = false;
	
	public EarlyOptimizer(ResolvedImport r, Optimizations opt){
		rim = r;
		
		this.opt = opt;
		
		constFolding = (opt & Optimizations.ConstantFolding) != 0;
		constBranching = (opt & Optimizations.ConstantBranching) != 0;
		exprSimplifier = (opt & Optimizations.ExpressionSimplification) != 0;
		deadCodeDel = (opt & Optimizations.EarlyDeadCodeElimination) != 0;
	}
	
	public ResolvedImport Optimize(){
		List<GlobalDeclStmt> globs = new(rim.globals.Length);
		foreach(GlobalDeclStmt v in rim.globals){
			GlobalDeclStmt bf = OptimizeGlobal(v);
			if(bf != null){
				globs.Add(bf);
			}
		}
		
		List<Stmt> top = new(rim.body.Length);
		top.AddRange(OptimizeMany(rim.body).Where(s => s != null));
		
		List<FunctionStmt> fs = new(rim.functions.Length);
		foreach(FunctionStmt fun in rim.functions){
			fs.Add(OptimizeFunc(fun));
		}
		
		return new ResolvedImport(rim.filename, rim.imports, globs.ToArray(), top.ToArray(), fs.ToArray());
	}
	
	Stmt[] OptimizeMany(Stmt[] n){
		anyChanged = true;
		while(anyChanged){
			anyChanged = false;
			
			if(exprSimplifier){
				n = n.Select(s => transformStmt(s, Optimizer.simplifyExpr, Optimizer.simplifyExpr)).ToArray();
			}
			
			if(constFolding){
				n = n.Select(s => transformStmt(s, none, Optimizer.foldConstants)).ToArray();
			}
			
			if(deadCodeDel){
				n = n.Select(s => transformStmt(s, deleteDeadCode, none)).ToArray();
			}
			
			if(constBranching){
				n = n.Select(s => transformStmt(s, branchConstant, none)).ToArray();
			}
		}
		
		return n;
	}
	
	//No transformation
	Stmt none(Stmt s) => s;
	Expr none(Expr e) => e;
	
	//Delete code after exit, return, break and continue
	Stmt deleteDeadCode(Stmt s){
		if(s is BlockStmt b){
			List<Stmt> c = new(b.inner.Length);
			foreach(Stmt t in b.inner){
				c.Add(t);
				if((t is ReturnStmt || t is ExitStmt || t is BreakStmt || t is ContinueStmt) && c.Count != b.inner.Length){
					anyChanged = true;
					return new BlockStmt(c.ToArray(), s.line);
				}
			}
		}
		return s;
	}
	
	//Select a branch if the condition is constant
	Stmt branchConstant(Stmt s){
		switch(s){
			case IfStmt f:
				if(f.condition is LiteralExpr lit){
					if(lit.val.Truthy){
						return f.then;
					}else{
						return f.els;
					}
				}
				break;
			
			case WhileStmt w:
				if(w.condition is LiteralExpr lit2){
					if(!lit2.val.Truthy){
						return w.els;
					}
				}
				break;
		}
		
		return s;
	}
	
	GlobalDeclStmt OptimizeGlobal(GlobalDeclStmt g){
		Expr x = g.val;
		
		anyChanged = true;
		while(anyChanged){
			anyChanged = false;
			
			if(exprSimplifier){
				x = transformExpr(x, Optimizer.simplifyExpr);
			}
			
			if(constFolding){
				x = transformExpr(x, Optimizer.foldConstants);
			}
		}
		
		return new GlobalDeclStmt(g.identifier, g.export, x, g.line);
	}
	
	FunctionStmt OptimizeFunc(FunctionStmt p){
		switch(p){
			case FunctionDefStmt f:
				return new FunctionDefStmt(f.identifier, f.pars, f.export, new BlockStmt(OptimizeMany(f.body.inner), f.body.line), p.line);
			
			default:
				return p;
		}
	}
	
	Stmt transformStmt(Stmt s, Func<Stmt, Stmt> stmtFunc, Func<Expr, Expr> exprFunc){
		s = s switch{
			ExprStmt e => new ExprStmt(transformExpr(e.exp, exprFunc), s.line),
			BlockStmt b => new BlockStmt(b.inner.Select(t => transformStmt(t, stmtFunc, exprFunc)).ToArray(), s.line),
			TabDeclStmt d => new TabDeclStmt(d.identifier, transformExpr(d.val, exprFunc), s.line),
			GlobalDeclStmt g => new GlobalDeclStmt(g.identifier, g.export, transformExpr(g.val, exprFunc), s.line),
			VarAssignStmt a => new VarAssignStmt(a.identifier, a.import, transformExpr(a.val, exprFunc), s.line),
			ElementAssignStmt l => new ElementAssignStmt(l.identifier, l.import, transformIndex(l.ind, exprFunc), transformExpr(l.val, exprFunc), s.line),
			IfStmt i => new IfStmt(transformExpr(i.condition, exprFunc), transformStmt(i.then, stmtFunc, exprFunc), transformStmt(i.els, stmtFunc, exprFunc), s.line),
			WhileStmt w => new WhileStmt(transformExpr(w.condition, exprFunc), transformStmt(w.body, stmtFunc, exprFunc), transformStmt(w.els, stmtFunc, exprFunc), s.line),
			DoStmt o => new DoStmt(transformExpr(o.condition, exprFunc), transformStmt(o.body, stmtFunc, exprFunc), transformStmt(o.els, stmtFunc, exprFunc), s.line),
			ForeachStmt f => new ForeachStmt(f.id, transformExpr(f.pool, exprFunc), (BlockStmt) transformStmt(f.body, stmtFunc, exprFunc), transformStmt(f.els, stmtFunc, exprFunc), s.line),
			ReturnStmt r => new ReturnStmt(transformExpr(r.val, exprFunc), s.line),
			
			_ => s
		};
		
		Stmt ns = stmtFunc(s);
		
		if(s != ns){
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
			CallExpr c => new CallExpr(c.identifier, c.import, c.self, c.args.Select(e => transformExpr(e, func)).ToArray()),
			
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