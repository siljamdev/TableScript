namespace TableScript;

/// <summary>
/// Statements that form programs
/// </summary>
public abstract record Stmt(int line){
	internal abstract string ToCompactString();
	
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
	
	internal override string ToCompactString(){
		return exp?.ToCompactString() + ";";
	}
}

//Dissapears in Binder
record BlockStmt(Stmt[] inner, int line) : Stmt(line){
	public override string ToString(){
		return ToBlockString() + ";";
	}
	
	internal override string ToCompactString(){
		return ToCompactBlockString() + ";";
	}
	
	internal override string ToBlockString(){
		return "{\n" + string.Join('\n', inner.SelectMany(h => h.ToString().Split("\n")).Select(h => "\t" + h)) + "\n}";
	}
	
	internal override string ToCompactBlockString(){
		return "{" + string.Join("", inner.Select(h => h.ToCompactString())) + "}";
	}
}

//Dissapears in binder
record TabDeclStmt(string identifier, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "tab " + identifier + " = " + val.ToString() + ";";
	}
	
	internal override string ToCompactString(){
		return "tab " + identifier + "=" + val.ToCompactString() + ";";
	}
}

//Dissapears in binder
record GlobalDeclStmt(string identifier, bool export, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return (export ? "export " : "") + "global " + identifier + " = " + val.ToString() + ";";
	}
	
	internal override string ToCompactString(){
		return (export ? "export " : "") + "global " + identifier + "=" + val.ToCompactString() + ";";
	}
}

//Dissapears in binder
record VarAssignStmt(string identifier, string import, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return (import != null ? (import + "::") : "") + identifier + " = " + val.ToString() + ";";
	}
	
	internal override string ToCompactString(){
		return (import != null ? (import + "::") : "") + identifier + "=" + val.ToCompactString() + ";";
	}
}

//Dissapears in binder
record ElementAssignStmt(string identifier, string import, IndexExpr ind, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return (import != null ? (import + "::") : "") + identifier + "[" + ind.ToString() + "] = " + val.ToString() + ";";
	}
	
	internal override string ToCompactString(){
		return (import != null ? (import + "::") : "") + identifier + "[" + ind.ToCompactString() + "]=" + val.ToCompactString() + ";";
	}
}

//Dissapears in binder
record IfStmt(Expr condition, Stmt then, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "if " + condition.ToString() + " " + then.ToBlockString() + (els != null ? " else " + els.ToBlockString() : "");
	}
	
	internal override string ToCompactString(){
		return "if " + condition.ToCompactString() + " " + then.ToCompactBlockString() + (els != null ? " else " + els.ToCompactBlockString() : "");
	}
}

//Dissapears in binder
record WhileStmt(Expr condition, Stmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "while " + condition.ToString() + " " + body.ToBlockString() + (els != null ? " else " + els.ToBlockString() : "");
	}
	
	internal override string ToCompactString(){
		return "while " + condition.ToCompactString() + " " + body.ToCompactBlockString() + (els != null ? " else " + els.ToCompactBlockString() : "");
	}
}

//Dissapears in binder
record DoStmt(Expr condition, Stmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "do " + body.ToBlockString() + " while " + condition.ToString() + (els != null ? " else " + els.ToBlockString() : ";");
	}
	
	internal override string ToCompactString(){
		return "do " + body.ToCompactBlockString() + " while " + condition.ToCompactString() + (els != null ? " else " + els.ToCompactBlockString() : ";");
	}
}

//Dissapears in binder
record ForeachStmt(string id, Expr pool, BlockStmt body, Stmt els, int line) : Stmt(line){
	public override string ToString(){
		return "foreach " + id + " @ " + pool.ToString() + body.ToBlockString() + (els != null ? " else " + els.ToString() : "");
	}
	
	internal override string ToCompactString(){
		return "foreach " + id + "@" + pool.ToCompactString() + body.ToCompactBlockString() + (els != null ? " else " + els.ToCompactBlockString() : "");
	}
}

//Dissapears in binder
record BreakStmt(int line) : Stmt(line){
	public override string ToString(){
		return "break;";
	}
	
	internal override string ToCompactString(){
		return "break;";
	}
}

//Dissapears in binder
record ContinueStmt(int line) : Stmt(line){
	public override string ToString(){
		return "continue;";
	}
	
	internal override string ToCompactString(){
		return "continue;";
	}
}

record ExitStmt(int line) : Stmt(line){
	public override string ToString(){
		return "exit;";
	}
	
	internal override string ToCompactString(){
		return "exit;";
	}
}

record ReturnStmt(Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "return " + val.ToString() + ";";
	}
	
	internal override string ToCompactString(){
		return "return " + val.ToCompactString() + ";";
	}
}

//Dissapears in resolver
record ImportStmt(string reference, int line) : Stmt(line){
	public override string ToString(){
		return "import \"" + reference + "\";";
	}
	
	internal override string ToCompactString(){
		return "import\"" + reference + "\";";
	}
}

#region functions
/// <summary>
/// Statement that represents a function
/// </summary>
public abstract record FunctionStmt(string identifier, string[] pars, int line) : Stmt(line){
	public int arity => pars.Length;
	
	internal abstract TabFunc ToTabFunc(string import, string filename);
}

//Dissapears in resolver
record FunctionDefStmt(string identifier, string[] pars, bool export, BlockStmt body, int line) : FunctionStmt(identifier, pars, line){
	public override string ToString(){
		return (export ? "export " : "") + "function " + identifier + "(" + string.Join(", ", pars) + ")" + body.ToBlockString();
	}
	
	internal override string ToCompactString(){
		return (export ? "export " : "") + "function " + identifier + "(" + string.Join(",", pars) + ")" + body.ToCompactBlockString();
	}
	
	internal override TabFunc ToTabFunc(string import, string filename){
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
	
	internal override string ToCompactString(){
		return "export function " + identifier + "(" + string.Join(",", pars) + "){}";
	}
	
	internal override TabFunc ToTabFunc(string import, string filename){
		return new TabExternFunc(import, identifier, pars, pars.Length > 0 && pars[0] == "self", true, body, description, filename, line);
	}
}
#endregion

#region bound
record BoundVarAssignStmt(int index, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "var_" + index  + " = " + val.ToString() + ";";
	}
	
	internal override string ToCompactString(){
		return "var_" + index  + "=" + val.ToCompactString() + ";";
	}
}

record BoundElementAssignStmt(int index, IndexExpr ind, Expr val, int line) : Stmt(line){
	public override string ToString(){
		return "var_" + index  + "[" + ind.ToString() + "] = " + val.ToString() + ";";
	}
	
	internal override string ToCompactString(){
		return "var_" + index  + "[" + ind.ToCompactString() + "]=" + val.ToCompactString() + ";";
	}
}
#endregion