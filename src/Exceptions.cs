using System;

namespace TableScript;

public class TableScriptException : Exception{
	public TableScriptErrorType type {get; private init;}
	public string filename {get; private init;}
	public int line {get; private init;}
	
	public TableScriptException(TableScriptErrorType t, string f, int l, string message) : base(message){
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
	
	static string typeName(TableScriptErrorType t){
		return t switch{
			TableScriptErrorType.Lexer => "LEX",
			TableScriptErrorType.Parser => "PAR",
			TableScriptErrorType.Resolver => "RES",
			TableScriptErrorType.Binder => "BIN",
			TableScriptErrorType.Optimizer => "OPT",
			TableScriptErrorType.Runtime => "RUN",
			_ => "???"
		};
	}
}

public enum TableScriptErrorType{
	Lexer, Parser, Resolver, Binder, Optimizer, Runtime
}