namespace TabScript;

/// <summary>
/// Statements that form programs
/// </summary>
public abstract record Stmt(int line){}

record ExprStmt(Expr exp, int line) : Stmt(line){
	public override string ToString(){
		return exp + ";";
	}
}

record BlockStmt(Stmt[] inner, int line) : Stmt(line){
	public override string ToString(){
		return "{\n" + string.Join('\n', inner.SelectMany(h => h.ToString().Split("\n")).Select(h => "\t" + h)) + "\n}";
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

record ElementAssignStmt(string identifier, IndexExpr ind, Expr val, int line) : Stmt(line){
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

#region functions
/// <summary>
/// Statement that represents a function
/// </summary>
public abstract record FunctionStmt(string identifier, string[] pars, int line) : Stmt(line){
	public int arity => pars.Length;
	
	public abstract TabFunc ToTabFunc(string import, string filename);
}

/// <summary>
/// Native function as a statement
/// </summary>
record FunctionDefStmt(string identifier, string[] pars, bool export, BlockStmt body, int line) : FunctionStmt(identifier, pars, line){
	public override string ToString(){
		return (export ? "export " : "") + "function " + identifier + "(" + string.Join(", ", pars) + ")" + body;
	}
	
	public override TabFunc ToTabFunc(string import, string filename){
		return new TabNativeFunc(import, identifier, pars, pars.Length > 0 && pars[0] == "self", export, body, filename, line);
	}
}

/// <summary>
/// External function as a statement
/// </summary>
public record FunctionExtStmt(string identifier, string[] pars, Func<Table[], Table> body, string description, int line) : FunctionStmt(identifier, pars, line){
	public FunctionExtStmt(string identifier, string[] pars, Func<Table[], Table> body, string description) : this(identifier, pars, body, description, -1){}
	
	public FunctionExtStmt(string identifier, string[] pars, Func<Table[], Table> body) : this(identifier, pars, body, null, -1){}
	
	public override string ToString(){
		return "export function " + identifier + "(" + string.Join(", ", pars) + "){ EXTERN; }" + (description == null ? "" : (" //" + description));
	}
	
	public override TabFunc ToTabFunc(string import, string filename){
		return new TabExternFunc(import, identifier, pars, pars.Length > 0 && pars[0] == "self", true, body, description, filename, line);
	}
}
#endregion

#region optimization
record OptVarDeclStmt(int depth, int index, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "tab " + depth + "_" + index + " = " + val + ";";
	}
}

record OptTabAssignStmt(int depth, int index, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return depth + "_" + index  + " = " + val + ";";
	}
}

record OptElementAssignStmt(int depth, int index, IndexExpr ind, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return depth + "_" + index  + "." + ind.ToString() + " = " + val + ";";
	}
}
#endregion