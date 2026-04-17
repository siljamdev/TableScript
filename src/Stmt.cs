namespace TabScript;

/// <summary>
/// Statements that form programs
/// </summary>
public abstract record Stmt(int line){
	public abstract string ToCompactString();
	
	internal virtual string ToBlockString(){
		return ToString();
	}
	
	internal virtual string ToCompactBlockString(){
		return ToCompactString();
	}
}

record ExprStmt(Expr exp, int line) : Stmt(line){
	public override string ToString(){
		return exp?.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return exp?.ToCompactString() + ";";
	}
}

record BlockStmt(Stmt[] inner, int line) : Stmt(line){
	public override string ToString(){
		return "{\n" + string.Join('\n', inner.SelectMany(h => h.ToString().Split("\n")).Select(h => "\t" + h)) + "\n};";
	}
	
	public override string ToCompactString(){
		return "{\n" + string.Join('\n', inner.Select(h => h.ToCompactString())) + "\n};";
	}
	
	internal override string ToBlockString(){
		return "{\n" + string.Join('\n', inner.SelectMany(h => h.ToString().Split("\n")).Select(h => "\t" + h)) + "\n}";
	}
	
	internal override string ToCompactBlockString(){
		return "{\n" + string.Join('\n', inner.Select(h => h.ToCompactString())) + "\n}";
	}
}

record VarDeclStmt(string identifier, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "tab " + identifier + " = " + val.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return "tab " + identifier + "=" + val.ToCompactString() + ";";
	}
}

record TabAssignStmt(string identifier, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return identifier + " = " + val.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return identifier + "=" + val.ToCompactString() + ";";
	}
}

record ElementAssignStmt(string identifier, IndexExpr ind, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return identifier + "[" + ind.ToString() + "] = " + val.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return identifier + "[" + ind.ToCompactString() + "]=" + val.ToCompactString() + ";";
	}
}

record IfStmt(Expr condition, Stmt then, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "if " + condition.ToString() + " " + then.ToBlockString() + (els != null ? " else " + els.ToBlockString() : "");
	}
	
	public override string ToCompactString(){
		return "if " + condition.ToCompactString() + " " + then.ToCompactBlockString() + (els != null ? " else " + els.ToCompactBlockString() : "");
	}
}

record WhileStmt(Expr condition, Stmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "while " + condition.ToString() + " " + body.ToBlockString() + (els != null ? " else " + els.ToBlockString() : "");
	}
	
	public override string ToCompactString(){
		return "while " + condition.ToCompactString() + " " + body.ToCompactBlockString() + (els != null ? " else " + els.ToCompactBlockString() : "");
	}
}

record DoStmt(Expr condition, Stmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "do " + body.ToBlockString() + " while " + condition.ToString() + (els != null ? " else " + els.ToBlockString() : ";");
	}
	
	public override string ToCompactString(){
		return "do " + body.ToCompactBlockString() + " while " + condition.ToCompactString() + (els != null ? " else " + els.ToCompactBlockString() : ";");
	}
}

record ForeachStmt(string id, Expr pool, BlockStmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "foreach " + id + " @ " + pool.ToString() + body.ToBlockString() + (els != null ? " else " + els.ToString() : "");
	}
	
	public override string ToCompactString(){
		return "foreach " + id + "@" + pool.ToCompactString() + body.ToCompactBlockString() + (els != null ? " else " + els.ToCompactBlockString() : "");
	}
}

record BreakStmt(int line) : Stmt(line){
	public override string ToString(){
		return "break;";
	}
	
	public override string ToCompactString(){
		return "break;";
	}
}

record ContinueStmt(int line) : Stmt(line){
	public override string ToString(){
		return "continue;";
	}
	
	public override string ToCompactString(){
		return "continue;";
	}
}

record ExitStmt(int line) : Stmt(line){
	public override string ToString(){
		return "exit;";
	}
	
	public override string ToCompactString(){
		return "exit;";
	}
}

record ReturnStmt(Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "return " + val.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return "return " + val.ToCompactString() + ";";
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
		return (export ? "export " : "") + "function " + identifier + "(" + string.Join(", ", pars) + ")" + body.ToBlockString();
	}
	
	public override string ToCompactString(){
		return (export ? "export " : "") + "function " + identifier + "(" + string.Join(",", pars) + ")" + body.ToCompactBlockString();
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
	
	public override string ToCompactString(){
		return "export function " + identifier + "(" + string.Join(",", pars) + "){}";
	}
	
	public override TabFunc ToTabFunc(string import, string filename){
		return new TabExternFunc(import, identifier, pars, pars.Length > 0 && pars[0] == "self", true, body, description, filename, line);
	}
}
#endregion

#region optimization
record OptVarDeclStmt(int depth, int index, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "tab " + depth + "_" + index + " = " + val.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return "tab " + depth + "_" + index + "=" + val.ToCompactString() + ";";
	}
}

record OptTabAssignStmt(int depth, int index, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return depth + "_" + index  + " = " + val.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return depth + "_" + index  + "=" + val.ToCompactString() + ";";
	}
}

record OptElementAssignStmt(int depth, int index, IndexExpr ind, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return depth + "_" + index  + "[" + ind.ToString() + "] = " + val.ToString() + ";";
	}
	
	public override string ToCompactString(){
		return depth + "_" + index  + "[" + ind.ToCompactString() + "]=" + val.ToCompactString() + ";";
	}
}
#endregion