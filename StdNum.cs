using System;

namespace TabScript;

public static class StdNum{
	public static FunctionStmt[] All => new FunctionStmt[]{toNum, toLen, sum, sub, mult, div, mod,
		floor, ceil, trunc, round, fract, abs, sign,
		sqrt, ln, log, exp, pow,
		getPi, sin, cos, tan, asin, acos, atan,
		min, max, clamp,
		greater, greaterEq, less, lessEq};
	
	public static readonly FunctionStmt toNum = new FunctionExtStmt("toNum", new string[]{"self"}, (Table[] t) => {
		return new Table(t[0].Length.ToString());
	}, -1);
	
	public static readonly FunctionStmt toLen = new FunctionExtStmt("toLen", new string[]{"self"}, (Table[] t) => {
		if(double.TryParse(t[0].AsString(), out double d)){
			return new Table((int) d);
		}
		return new Table(0);
	}, -1);
	
	public static readonly FunctionStmt sum = new FunctionExtStmt("sum", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table((a + b).ToString());
	}, -1);
	
	public static readonly FunctionStmt sub = new FunctionExtStmt("sub", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table((a - b).ToString());
	}, -1);
	
	public static readonly FunctionStmt mult = new FunctionExtStmt("mult", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table((a * b).ToString());
	}, -1);
	
	public static readonly FunctionStmt div = new FunctionExtStmt("div", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table((a / b).ToString());
	}, -1);
	
	public static readonly FunctionStmt mod = new FunctionExtStmt("mod", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table((a % b).ToString());
	}, -1);
	
	public static readonly FunctionStmt floor = new FunctionExtStmt("floor", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Floor(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt ceil = new FunctionExtStmt("ceil", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Ceiling(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt trunc = new FunctionExtStmt("trunc", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Truncate(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt round = new FunctionExtStmt("round", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Round(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt fract = new FunctionExtStmt("fract", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table((a - Math.Floor(a)).ToString());
	}, -1);
	
	public static readonly FunctionStmt abs = new FunctionExtStmt("abs", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Abs(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt sign = new FunctionExtStmt("sign", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Sign(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt sqrt = new FunctionExtStmt("sqrt", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Sqrt(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt ln = new FunctionExtStmt("ln", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Log(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt log = new FunctionExtStmt("log", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Log10(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt exp = new FunctionExtStmt("exp", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Exp(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt pow = new FunctionExtStmt("pow", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table(Math.Pow(a, b).ToString());
	}, -1);
	
	public static readonly FunctionStmt getPi = new FunctionExtStmt("getPi", new string[]{}, (Table[] t) => {
		return new Table(Math.PI.ToString());
	}, -1);
	
	public static readonly FunctionStmt sin = new FunctionExtStmt("sin", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Sin(a).ToString());
	}, -1);

	public static readonly FunctionStmt cos = new FunctionExtStmt("cos", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Cos(a).ToString());
	}, -1);

	public static readonly FunctionStmt tan = new FunctionExtStmt("tan", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Tan(a).ToString());
	}, -1);

	public static readonly FunctionStmt asin = new FunctionExtStmt("asin", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Asin(a).ToString());
	}, -1);

	public static readonly FunctionStmt acos = new FunctionExtStmt("acos", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Acos(a).ToString());
	}, -1);

	public static readonly FunctionStmt atan = new FunctionExtStmt("atan", new string[]{"num"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		return new Table(Math.Atan(a).ToString());
	}, -1);
	
	public static readonly FunctionStmt min = new FunctionExtStmt("min", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table(Math.Min(a, b).ToString());
	}, -1);
	
	public static readonly FunctionStmt max = new FunctionExtStmt("max", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table(Math.Max(a, b).ToString());
	}, -1);
	
	public static readonly FunctionStmt clamp = new FunctionExtStmt("clamp", new string[]{"num", "min", "max"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[2].AsString(), out double c)){
			return new Table(0);
		}
		
		return new Table(Math.Clamp(a, b, c).ToString());
	}, -1);
	
	public static readonly FunctionStmt greater = new FunctionExtStmt("greater", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table(a > b ? Table.True : Table.False);
	}, -1);
	
	public static readonly FunctionStmt greaterEq = new FunctionExtStmt("greaterEq", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table(a >= b ? Table.True : Table.False);
	}, -1);
	
	public static readonly FunctionStmt less = new FunctionExtStmt("less", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table(a < b ? Table.True : Table.False);
	}, -1);
	
	public static readonly FunctionStmt lessEq = new FunctionExtStmt("lessEq", new string[]{"num1", "num2"}, (Table[] t) => {
		if(!double.TryParse(t[0].AsString(), out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t[1].AsString(), out double b)){
			return new Table(0);
		}
		
		return new Table(a <= b ? Table.True : Table.False);
	}, -1);
}