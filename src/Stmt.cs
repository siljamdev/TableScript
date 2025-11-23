namespace TabScript;

public abstract record Stmt(int line){
	
}

record ExprStmt(Expr exp, int line) : Stmt(line){
	public override string ToString(){
		return exp + ";";
	}
}

record BlockStmt(Stmt[] inner, int line) : Stmt(line){
	public override string ToString(){
		return "{\n" + string.Join('\n', inner.Select(h => h.ToString())) + "\n}";
	}
}

record VarDeclStmt(string identifier, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "tab " + identifier + " = " + val + ";";
	}
}

record TabAssignStmt(string identifier, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return identifier + " = " + val + ";";
	}
}

record ElementAssignStmt(string identifier, TabIndex ind, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return identifier + "." + ind.ToString() + " = " + val + ";";
	}
}

record IfStmt(Expr condition, Stmt then, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "if " + condition + " " + then + (els != null ? " else " + els : "");
	}
}

record WhileStmt(Expr condition, Stmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "while " + condition + " " + body + (els != null ? " else " + els : "");
	}
}

record DoStmt(Expr condition, Stmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "do " + body + " while " + condition + (els != null ? " else " + els : ";");
	}
}

record ForeachStmt(string id, Expr pool, BlockStmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "foreach " + id + " @ " + pool + body + (els != null ? " else " + els : "");
	}
}

record BreakStmt(int line) : Stmt(line){
	public override string ToString(){
		return "break;";
	}
}

record ContinueStmt(int line) : Stmt(line){
	public override string ToString(){
		return "continue;";
	}
}

record ExitStmt(int line) : Stmt(line){
	public override string ToString(){
		return "exit;";
	}
}

record ReturnStmt(Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "return " + val + ";";
	}
}

record ImportStmt(string import, int line) : Stmt(line){
	public override string ToString(){
		return "import " + import + ";";
	}
}

#region function
public abstract record FunctionStmt(string identifier, string[] pars, int line) : Stmt(line){
	public int arity => pars.Length;
	
	public abstract TabFunc ToTabFunc(string im);
}

record FunctionDefStmt(string identifier, string[] pars, BlockStmt body, int line) : FunctionStmt(identifier, pars, line){
	public override string ToString(){
		return "function " + identifier + "(" + string.Join(", ", pars) + ")" + body;
	}
	
	public override TabFunc ToTabFunc(string im){
		return new TabNativeFunc(im, identifier, pars, pars.Length > 0 && pars[0] == "self", body, line);
	}
}

public record FunctionExtStmt(string identifier, string[] pars, Func<Table[], Table> body, int line) : FunctionStmt(identifier, pars, line){
	public override string ToString(){
		return "function " + identifier + "(" + string.Join(", ", pars) + "){ ##NATIVE##; }";
	}
	
	public override TabFunc ToTabFunc(string im){
		return new TabExternFunc(im, identifier, pars, pars.Length > 0 && pars[0] == "self", body, line);
	}
}
#endregion

#region optimization
record OptVarDeclStmt(int depth, int index, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "tab " + depth + ":" + index + " = " + val + ";";
	}
}

record OptTabAssignStmt(int depth, int index, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return depth + ":" + index  + " = " + val + ";";
	}
}

record OptElementAssignStmt(int depth, int index, TabIndex ind, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return depth + ":" + index  + "." + ind.ToString() + " = " + val + ";";
	}
}
#endregion