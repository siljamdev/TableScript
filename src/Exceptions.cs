using System;

public class TabScriptException : Exception{
	public TabScriptErrorType type {get; private init;}
	public string filename {get; private init;}
	public int line {get; private init;}
	
	public TabScriptException(TabScriptErrorType t, string f, int l, string message) : base(message){
		filename = f;
		type = t;
		line = l;
	}
	
	public override string ToString(){
		return "[ERROR] [" + typeName(type) + "] Filename: '" + filename + "' Line: " + line + "\n" + base.ToString();
	}
	
	public string ToShortString(){
		return "[ERROR] [" + typeName(type) + "] Filename: '" + filename + "' Line: " + line + "\n\t" + Message; 
	}
	
	static string typeName(TabScriptErrorType t){
		return t switch{
			TabScriptErrorType.Lexer => "LEX",
			TabScriptErrorType.Parser => "PAR",
			TabScriptErrorType.Resolver => "RES",
			TabScriptErrorType.Binder => "BIN",
			TabScriptErrorType.Optimizer => "OPT",
			TabScriptErrorType.Runtime => "RUN",
			_ => "???"
		};
	}
}

public enum TabScriptErrorType{
	Lexer, Parser, Resolver, Binder, Optimizer, Runtime
}