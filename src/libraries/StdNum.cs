using System;

namespace TabScript;

/// <summary>
/// Numbers are `double`, and are stored in its string representation
/// All functions have the same name in the code and in here
/// </summary>
public static class StdNum{
	
	public static (Delegate func, string description)[] AllFunctions => new (Delegate, string)[]{
		(toNum, "Transform length into num"),
		(toLen, "Transform num into length"),
		(sum, "Sum two nums"),
		(sub, "Subtract two nums"),
		(mult, "Multiply two nums"),
		(div, "Divide two nums"),
		(mod, "Modulus operation (division remainder) of two nums"),
		(floor, "Floor operation on a num"),
		(ceil, "Ceil operation on a num"),
		(trunc, "Truncate a num"),
		(round, "Round a num"),
		(fract, "Get fractionary part of a num"),
		(abs, "Absolute value of a num"),
		(sign, "Sign of a num"),
		(sqrt, "Square root of a num"),
		(ln, "Natural logarithm of a num"),
		(log, "Decimal logarithm of a num"),
		(exp, "Eulers number to the power of a num"),
		(pow, "num1 to the power of num2"),
		(getPi, "Gets Pi constant"),
		(getE, "Gets e (Eulers number) constant"),
		(sin, "Sine of a num"),
		(cos, "Cosine of a num"),
		(tan, "Tangent of a num"),
		(asin, "Inverse sine of a num"),
		(acos, "Inverse cosine of a num"),
		(atan, "Inverse tangent of a num"),
		(min, "Minimum of two nums"),
		(max, "Maximum of two nums"),
		(clamp, "Clamp num between min and max"),
		(greater, "True if num1 is greater that num2, false otherwise"),
		(greaterEq, "True if num1 is greater or equal than num2, false otherwise"),
		(less, "True if num1 is less that num2, false otherwise"),
		(lessEq, "True if num1 is less or equal than num2, false otherwise"),
		(range, "Generate a range of numbers between two limits"),
		(rand, "Random number between two limits"),
	};
	
	static ResolvedImport compiled = null;
	
	public static ResolvedImport AsImport {get{
		if(compiled == null){
			compiled = Library.BuildLibrary("stdnum", AllFunctions);
		}
		return compiled;
	}}
	
	/// <summary>
	/// Transform length into num
	/// </summary>
	public static string toNum(int self){
		return ((double) self).ToStr();
	}
	
	/// <summary>
	/// Transform num into length
	/// </summary>
	public static int toLen(string self){
		return double.TryParse(self, out double d) ? (int) d : 0;
	}
	
	/// <summary>
	/// Sum two nums
	/// </summary>
	public static Table sum(string num1, string num2) => numOp(num1, num2, (a, b) => a + b);
	
	/// <summary>
	/// Subtract two nums
	/// </summary>
	public static Table sub(string num1, string num2) => numOp(num1, num2, (a, b) => a - b);
	
	/// <summary>
	/// Multiply two nums
	/// </summary>
	public static Table mult(string num1, string num2) => numOp(num1, num2, (a, b) => a * b);
	
	/// <summary>
	/// Divide two nums
	/// </summary>
	public static Table div(string num1, string num2) => numOp(num1, num2, (a, b) => a / b);
	
	/// <summary>
	/// Modulus operation (division remainder) of two nums
	/// </summary>
	public static Table mod(string num1, string num2) => numOp(num1, num2, (a, b) => a % b);
	
	/// <summary>
	/// Floor operation on a num
	/// </summary>
	public static Table floor(string num) => numOp(num, a => Math.Floor(a));
	
	/// <summary>
	/// Ceil operation on a num
	/// </summary>
	public static Table ceil(string num) => numOp(num, a => Math.Ceiling(a));
	
	/// <summary>
	/// Truncate a num
	/// </summary>
	public static Table trunc(string num) => numOp(num, a => Math.Truncate(a));
	
	/// <summary>
	/// Round a num
	/// </summary>
	public static Table round(string num) => numOp(num, a => Math.Round(a));
	
	/// <summary>
	/// Get fractionary part of a num
	/// </summary>
	public static Table fract(string num) => numOp(num, a => a - Math.Floor(a));
	
	/// <summary>
	/// Absolute value of a num
	/// </summary>
	public static Table abs(string num) => numOp(num, a => Math.Abs(a));
	
	/// <summary>
	/// Sign of a num
	/// </summary>
	public static Table sign(string num) => numOp(num, a => Math.Sign(a));
	
	/// <summary>
	/// Square root of a num
	/// </summary>
	public static Table sqrt(string num) => numOp(num, a => Math.Sqrt(a));
	
	/// <summary>
	/// Natural logarithm of a num
	/// </summary>
	public static Table ln(string num) => numOp(num, a => Math.Log(a));
	
	/// <summary>
	/// Decimal logarithm of a num
	/// </summary>
	public static Table log(string num) => numOp(num, a => Math.Log10(a));
	
	/// <summary>
	/// Eulers number to the power of a num
	/// </summary>
	public static Table exp(string num) => numOp(num, a => Math.Exp(a));
	
	/// <summary>
	/// num1 to the power of num2
	/// </summary>
	public static Table pow(string num1, string num2) => numOp(num1, num2, (a, b) => Math.Pow(a, b));
	
	/// <summary>
	/// Gets Pi constant
	/// </summary>
	public static string getPi() => Math.PI.ToStr();
	
	/// <summary>
	/// Gets e (Eulers number) constant
	/// </summary>
	public static string getE() => Math.E.ToStr();
	
	/// <summary>
	/// Sine of a num
	/// </summary>
	public static Table sin(string num) => numOp(num, a => Math.Sin(a));
	
	/// <summary>
	/// Cosine of a num
	/// </summary>
	public static Table cos(string num) => numOp(num, a => Math.Cos(a));
	
	/// <summary>
	/// Tangent of a num
	/// </summary>
	public static Table tan(string num) => numOp(num, a => Math.Tan(a));
	
	/// <summary>
	/// Inverse sine of a num
	/// </summary>
	public static Table asin(string num) => numOp(num, a => Math.Asin(a));
	
	/// <summary>
	/// Inverse cosine of a num
	/// </summary>
	public static Table acos(string num) => numOp(num, a => Math.Acos(a));
	
	/// <summary>
	/// Inverse tangent of a num
	/// </summary>
	public static Table atan(string num) => numOp(num, a => Math.Atan(a));
	
	/// <summary>
	/// Minimum of two nums
	/// </summary>
	public static Table min(string num1, string num2) => numOp(num1, num2, (a, b) => Math.Min(a, b));
	
	/// <summary>
	/// Maximum of two nums
	/// </summary>
	public static Table max(string num1, string num2) => numOp(num1, num2, (a, b) => Math.Max(a, b));
	
	/// <summary>
	/// Clamp num between min and max
	/// </summary>
	public static Table clamp(string num, string min, string max) => numOp(num, min, max, (a, b, c) => Math.Clamp(a, b, c));
	
	/// <summary>
	/// True if num1 is greater that num2, false otherwise
	/// </summary>
	public static Table greater(string num1, string num2) => numEq(num1, num2, (a, b) => a > b);
	
	/// <summary>
	/// True if num1 is greater or equal than num2, false otherwise
	/// </summary>
	public static Table greaterEq(string num1, string num2) => numEq(num1, num2, (a, b) => a >= b);
	
	/// <summary>
	/// True if num1 is less that num2, false otherwise
	/// </summary>
	public static Table less(string num1, string num2) => numEq(num1, num2, (a, b) => a < b);
	
	/// <summary>
	/// True if num1 is less or equal than num2, false otherwise
	/// </summary>
	public static Table lessEq(string num1, string num2) => numEq(num1, num2, (a, b) => a <= b);
	
	/// <summary>
	/// Generate a range of numbers between two limits
	/// </summary>
	public static Table range(string start, string end, string step){
		if(!double.TryParse(start, out double start2)){
			return new Table(0);
		}
		
		if(!double.TryParse(end, out double end2)){
			return new Table(0);
		}
		
		if(!double.TryParse(step, out double step2) || step2 == 0d){
			return new Table(0);
		}
		
		if(start2 > end2){
			double t222 = end2;
			end2 = start2;
			start2 = t222;
		}
		
		step2 = Math.Max(step2, -step2);
		
		List<double> l = new();
		
		for(double d = start2; d <= end2; d += step2){
			l.Add(d);
		}
		
		return new Table(l.Select(h => h.ToStr()).ToArray());
	}
	
	/// <summary>
	/// Random number between two limits
	/// </summary>
	public static Table rand(string min, string max){
		if(!double.TryParse(min, out double min2)){
			return new Table(0);
		}
		
		if(!double.TryParse(max, out double max2)){
			return new Table(0);
		}
		
		if(min2 > max2){
			return new Table(0);
		}
		
		return new Table(randomD(min2, max2).ToStr());
	}
	
	//helpers
	
	static double randomD(double n, double x){
		return n + (x - n) * Random.Shared.NextDouble();
	}
	
	static Table numOp(string t, Func<double, double> op){
		return double.TryParse(t, out double a) ? new Table(op(a).ToStr()) : new Table(0);
	}
	
	static Table numOp(string t1, string t2, Func<double, double, double> op){
		if(!double.TryParse(t1, out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t2, out double b)){
			return new Table(0);
		}
		
		return new Table(op(a, b).ToStr());
	}

	static Table numOp(string t1, string t2, string t3, Func<double, double, double, double> op){
		if(!double.TryParse(t1, out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t2, out double b)){
			return new Table(0);
		}
		
		if(!double.TryParse(t3, out double c)){
			return new Table(0);
		}
		
		return new Table(op(a, b, c).ToStr());
	}
	
	static Table numEq(string t1, string t2, Func<double, double, bool> op){
		if(!double.TryParse(t1, out double a)){
			return new Table(0);
		}
		
		if(!double.TryParse(t2, out double b)){
			return new Table(0);
		}
		
		return Table.GetBool(op(a, b));
	}
	
	static string ToStr(this double d){
		return d.ToString();
	}
}
