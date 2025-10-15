using System;

public class TabScriptException : Exception{
	public TabScriptErrorType type {get; private init;}
	public int line {get; private init;}
	
	public TabScriptException(TabScriptErrorType t, int l, string mess) : base(mess){
		type = t;
		line = l;
	}
	
	public override string ToString(){
		return "[ERROR] [" + typeName(type) + "] Line: " + line + "\n" + base.ToString();
	}
	
	public string ToShortString(){
		return "[ERROR] [" + typeName(type) + "] Line: " + line + "\n\t" + Message; 
	}
	
	static string typeName(TabScriptErrorType t){
		return t switch{
			TabScriptErrorType.Lexer => "LEX",
			TabScriptErrorType.Parser => "PAR",
			TabScriptErrorType.Resolver => "RES",
			TabScriptErrorType.Checker => "CHK",
			TabScriptErrorType.Optimizer => "OPT",
			TabScriptErrorType.Runtime => "RUN",
			_ => "NUL"
		};
	}
}

public enum TabScriptErrorType{
	Lexer, Parser, Resolver, Checker, Optimizer, Runtime
}