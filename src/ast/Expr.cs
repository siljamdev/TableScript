namespace TableScript;

abstract record Expr{
	public virtual bool hasSideEffects(){
		return false;
	}
	
	public virtual int precedence(){
		return 0;
	}
	
	public string ToCompactString(int parentPrecedence = 0){
		if(precedence() <= parentPrecedence){
			return "(" + ToCompactStr() + ")";
		}
		
		return ToCompactStr();
	}
	
	public abstract string ToCompactStr();
}

record BinaryExpr(Expr left, TokenType op, Expr right) : Expr{
	public override string ToString(){
		return left?.ToString() + " " + Token.GetAsString(op) + " " + right?.ToString();
	}
	
	public override string ToCompactStr(){
		return left?.ToCompactString(precedence()) + Token.GetAsString(op) + (op == TokenType.Minus ? " " : "") + right?.ToCompactString(precedence());
	}
	
	public override bool hasSideEffects(){
		return left.hasSideEffects() || right.hasSideEffects();
	}
	
	public override int precedence(){
		return op switch{
			TokenType.Star => 700,
			TokenType.Plus => 600,
			TokenType.Minus => 600,
			TokenType.At => 500,
			TokenType.Greater => 400,
			TokenType.GreaterEqual => 400,
			TokenType.Less => 400,
			TokenType.LessEqual => 400,
			TokenType.DobEqual => 300,
			TokenType.ExclamationEqual => 300,
			TokenType.And => 200,
			TokenType.Or => 100,
			_ => 0
		};
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
	
	public override string ToCompactStr(){
		if(op == TokenType.Exclamation){
			return Token.GetAsString(op) + right?.ToCompactString(precedence());
		}else{
			return right?.ToCompactString(precedence()) + Token.GetAsString(op);
		}
	}
	
	public override bool hasSideEffects(){
		return right.hasSideEffects();
	}
	
	public override int precedence(){
		return op switch{
			TokenType.Caret => 900,
			TokenType.Percentage => 900,
			TokenType.Exclamation => 800,
			_ => 0
		};
	}
}

record TernaryExpr(Expr cond, Expr tr, Expr fa) : Expr{
	public override string ToString(){
		return cond?.ToString() + " ? " + tr?.ToString() + " : " + fa?.ToString();
	}
	
	public override string ToCompactStr(){
		return cond?.ToCompactString(precedence()) + "?" + tr?.ToCompactString(precedence()) + ":" + fa?.ToCompactString(precedence());
	}
	
	public override bool hasSideEffects(){
		return cond.hasSideEffects() || tr.hasSideEffects() || fa.hasSideEffects();
	}
	
	public override int precedence(){
		return 50;
	}
}

record GetElementExpr(Expr left, IndexExpr ind) : Expr{
	public override string ToString(){
		return left?.ToString() + "[" + ind.ToString() + "]";
	}
	
	public override string ToCompactStr(){
		return left?.ToCompactString(precedence()) + "[" + ind.ToCompactString() + "]";
	}
	
	public override bool hasSideEffects(){
		return left.hasSideEffects() || ind.hasSideEffects();
	}
	
	public override int precedence(){
		return 1000;
	}
}

record GetRangeExpr(Expr left, IndexExpr ind, IndexExpr len) : Expr{
	public override string ToString(){
		return left?.ToString() + "[" + ind.ToString() + ", " + len.ToString() + "]";
	}
	
	public override string ToCompactStr(){
		return left?.ToCompactString(precedence()) + "[" + ind.ToCompactString() + "," + len.ToCompactString() + "]";
	}
	
	public override bool hasSideEffects(){
		return left.hasSideEffects() || ind.hasSideEffects() || len.hasSideEffects();
	}
	
	public override int precedence(){
		return 1000;
	}
}

record IndexExpr(TabIndex ind, Expr val) : Expr{
	public override string ToString(){
		return val == null ? ind.ToString() : val.ToString();
	}
	
	public override string ToCompactStr(){
		return val == null ? ind.ToString() : val.ToCompactString();
	}
	
	public override bool hasSideEffects(){
		return val?.hasSideEffects() ?? false;
	}
	
	public override int precedence(){
		return 1000;
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
	
	public override string ToCompactStr(){
		return (import != null ? (import + "::") : "") + identifier + "(" + string.Join(",", args.Select(a => a.ToCompactString())) + ")";
	}
	
	public override bool hasSideEffects(){
		return true;
	}
	
	public override int precedence(){
		return 1000;
	}
}

record LiteralExpr(Table val) : Expr{
	public override string ToString(){
		return val?.ToString();
	}
	
	public override string ToCompactStr(){
		return val?.ToCompactString();
	}
	
	public override int precedence(){
		return 1000;
	}
}

record BuildLiteralExpr(Expr[] parts) : Expr{
	public override string ToString(){
		return "[" + string.Join(", ", parts.Select(p => p.ToString())) + "]";
	}
	
	public override string ToCompactStr(){
		return "[" + string.Join(",", parts.Select(p => p.ToCompactString())) + "]";
	}
	
	public override bool hasSideEffects(){
		return parts.Any(p => p.hasSideEffects());
	}
	
	public override int precedence(){
		return 1000;
	}
}

record VariableExpr(string identifier, string import) : Expr{
	public override string ToString(){
		return (import != null ? (import + "::") : "") + identifier;
	}
	
	public override string ToCompactStr(){
		return (import != null ? (import + "::") : "") + identifier;
	}
	
	public override int precedence(){
		return 1000;
	}
}

#region bound
record BoundCallExpr(int index, Expr[] args) : Expr{
	public override string ToString(){
		return "func_" + index + "(" + string.Join(", ", args.Select(a => a.ToString())) + ")";
	}
	
	public override string ToCompactStr(){
		return "func_" + index + "(" + string.Join(",", args.Select(a => a.ToCompactString())) + ")";
	}
	
	public override bool hasSideEffects(){
		return true;
	}
	
	public override int precedence(){
		return 1000;
	}
	
	protected override Type EqualityContract => typeof(BoundCallExpr);
	
	public virtual bool Equals(BoundCallExpr? b){
		if(b == null)
			return false;
		
		if(index != b.index)
			return false;
		
		if(args == null && b.args == null)
			return true;
		
		if(args == null || b.args == null)
			return false;
		
		if(args.Length != b.args.Length)
			return false;
		
		for(int i = 0; i < args.Length; i++){
			if(args[i] != b.args[i])
				return false;
		}
		
		return true;
	}
}

record BoundVariableExpr(int index) : Expr{
	public override string ToString(){
		return "var_" + index;
	}
	
	public override string ToCompactStr(){
		return "var_" + index;
	}
	
	public override int precedence(){
		return 1000;
	}
}
#endregion