namespace TabScript;

abstract record Expr{
}

record BinaryExpr(Expr left, TokenType op, Expr right) : Expr{
	public override string ToString(){
		return left?.ToString() + " " + Token.GetAsString(op) + " " + right?.ToString();
	}
}

record GroupingExpr(Expr exp) : Expr{
	public override string ToString(){
		return "(" + exp?.ToString() + ")";
	}
}

record UnaryExpr(TokenType op, Expr right) : Expr{
	public override string ToString(){
		return Token.GetAsString(op) + right?.ToString();
	}
}

record TernaryExpr(Expr cond, Expr tr, Expr fa) : Expr{
	public override string ToString(){
		return cond?.ToString() + " ? " + tr?.ToString() + " : " + fa?.ToString();
	}
}

record GetElementExpr(Expr left, TabIndex ind) : Expr{
	public override string ToString(){
		return left?.ToString() + "." + ind.ToString();
	}
}

record GetRangeExpr(Expr left, IndexExpr ind, IndexExpr len) : Expr{
	public override string ToString(){
		return left?.ToString() + "[" + ind.ToString() + "," + len.ToString() + "]";
	}
}

record IndexExpr(TabIndex ind, Expr val) : Expr{
	public override string ToString(){
		return val == null ? ind.ToString() : val.ToString();
	}
}

//Null imports meand any
record CallExpr(string identifier, string import, bool self, Expr[] args) : Expr{
	public int arity => args.Length;
	
	public override string ToString(){
		return self ? (args[0] + "." + (import != null ? (import + "::") : "") + identifier + "(" + string.Join(", ", args.Skip(1).Select(a => a.ToString())) + ")") : ((import != null ? (import + "::") : "") + identifier + "(" + string.Join(", ", args.Select(a => a.ToString())) + ")");
	}
}

record LiteralExpr(Table val) : Expr{
	public override string ToString(){
		return val?.ToString();
	}
}

record BuildLiteralExpr(Expr[] parts) : Expr{
	public override string ToString(){
		return "º[" + string.Join(", ", parts.Select(p => p.ToString())) + "]º";
	}
}

record VariableExpr(string identifier) : Expr{
	public override string ToString(){
		return identifier;
	}
}

record DollarExpr(Expr[] parts) : Expr{
	public override string ToString(){
		return "$\"" + string.Join(" ", parts.Select(p => p.ToString())) + "\"";
	}
}

#region optimized
record OptCallExpr(int index, Expr[] args) : Expr{
	public override string ToString(){
		return "_:" + index + "(" + string.Join(", ", args.Select(a => a.ToString())) + ")";
	}
}

record OptVariableExpr(int depth, int index) : Expr{
	public override string ToString(){
		return depth + ":" + index;
	}
}
#endregion