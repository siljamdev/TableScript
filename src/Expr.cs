namespace TabScript;

abstract record Expr{
	public abstract string ToCompactString();
}

record BinaryExpr(Expr left, TokenType op, Expr right) : Expr{
	public override string ToString(){
		return left?.ToString() + " " + Token.GetAsString(op) + " " + right?.ToString();
	}
	
	public override string ToCompactString(){
		return "(" + left?.ToCompactString() + Token.GetAsString(op) + (op == TokenType.Minus ? " " : "") + right?.ToCompactString() + ")";
	}
}

record UnaryExpr(TokenType op, Expr right) : Expr{
	public override string ToString(){
		if(op == TokenType.Exclamation){
			return Token.GetAsString(op) + right?.ToString();
		}else{
			return right?.ToString() + Token.GetAsString(op);
		}
	}
	
	public override string ToCompactString(){
		if(op == TokenType.Exclamation){
			return "(" + Token.GetAsString(op) + right?.ToCompactString() + ")";
		}else{
			return "(" + right?.ToCompactString() + Token.GetAsString(op) + ")";
		}
	}
}

record TernaryExpr(Expr cond, Expr tr, Expr fa) : Expr{
	public override string ToString(){
		return cond?.ToString() + " ? " + tr?.ToString() + " : " + fa?.ToString();
	}
	
	public override string ToCompactString(){
		return "(" + cond?.ToCompactString() + "?" + tr?.ToCompactString() + ":" + fa?.ToCompactString() + ")";
	}
}

record GetElementExpr(Expr left, IndexExpr ind) : Expr{
	public override string ToString(){
		return left?.ToString() + "[" + ind.ToString() + "]";
	}
	
	public override string ToCompactString(){
		return left?.ToCompactString() + "[" + ind.ToCompactString() + "]";
	}
}

record GetRangeExpr(Expr left, IndexExpr ind, IndexExpr len) : Expr{
	public override string ToString(){
		return left?.ToString() + "[" + ind.ToString() + ", " + len.ToString() + "]";
	}
	
	public override string ToCompactString(){
		return left?.ToCompactString() + "[" + ind.ToCompactString() + "," + len.ToCompactString() + "]";
	}
}

record IndexExpr(TabIndex ind, Expr val) : Expr{
	public override string ToString(){
		return val == null ? ind.ToString() : val.ToString();
	}
	
	public override string ToCompactString(){
		return val == null ? ind.ToString() : val.ToCompactString();
	}
}

//Null imports means any
record CallExpr(string identifier, string import, bool self, Expr[] args) : Expr{
	public int arity => args.Length;
	
	public override string ToString(){
		if(self){
			return args[0] + "." + (import != null ? (import + "::") : "") + identifier + "(" + string.Join(", ", args.Skip(1).Select(a => a.ToString())) + ")";
		}else{
			return (import != null ? (import + "::") : "") + identifier + "(" + string.Join(", ", args.Select(a => a.ToString())) + ")";
		}
	}
	
	public override string ToCompactString(){
		return (import != null ? (import + "::") : "") + identifier + "(" + string.Join(",", args.Select(a => a.ToCompactString())) + ")";
	}
}

record LiteralExpr(Table val) : Expr{
	public override string ToString(){
		return val?.ToString();
	}
	
	public override string ToCompactString(){
		return val?.ToCompactString();
	}
}

record BuildLiteralExpr(Expr[] parts) : Expr{
	public override string ToString(){
		return "[" + string.Join(", ", parts.Select(p => p.ToString())) + "]";
	}
	
	public override string ToCompactString(){
		return "[" + string.Join(",", parts.Select(p => p.ToCompactString())) + "]";
	}
}

record VariableExpr(string identifier) : Expr{
	public override string ToString(){
		return identifier;
	}
	
	public override string ToCompactString(){
		return identifier;
	}
}

#region optimized
record OptCallExpr(int index, Expr[] args) : Expr{
	public override string ToString(){
		return "@_" + index + "(" + string.Join(", ", args.Select(a => a.ToString())) + ")";
	}
	
	public override string ToCompactString(){
		return "@_" + index + "(" + string.Join(",", args.Select(a => a.ToCompactString())) + ")";
	}
}

record OptVariableExpr(int depth, int index) : Expr{
	public override string ToString(){
		return depth + "_" + index;
	}
	
	public override string ToCompactString(){
		return depth + "_" + index;
	}
}
#endregion