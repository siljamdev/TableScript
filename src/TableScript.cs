using System;

namespace TabScript;

public class TableScript{
	public Snippet body {get; private set;}
	public TabFunc[] functions {get; private set;}
	
	public Action<TabScriptException> OnReport;
	
	Interpreter i;
	
	internal TableScript(Snippet bod, TabFunc[] funcs, Action<TabScriptException> report = null){
		body = bod;
		functions = funcs;
		OnReport = report;
		
		i = new Interpreter(this);
	}
	
	public void Run(IEnumerable<string> args){
		Run(new Table(new List<string>(args ?? Enumerable.Empty<string>())));
	}
	
	public void Run(Table args = null){
		if(i.interpreted){
			i = new Interpreter(this);
		}
		
		try{
			i.Interpret(args ?? new Table());
		}catch(TabScriptException ex){
			OnReport(ex);
		}
	}
	
	/// <summary>
	/// Always call after running!
	/// </summary>
	public Table CallFunction(string import, string identifier, params Table[] args){
		try{
			return i.CallFunction(import, identifier, args ?? Array.Empty<Table>());
		}catch(TabScriptException ex){
			OnReport(ex);
			return new Table(0);
		}
	}
	
	public override string ToString(){
		return body.ToString() + "\n" + string.Join("\n", functions.Select((h, i) => "@_" + i + " " + h.ToString()));
	}
	
	//######################################################################
	
	static void defaultReport(TabScriptException tsex){
		Console.Error.WriteLine(tsex.ToShortString());
	}
	
	public static TableScript FromSource(string filename, string src){
		return FromSource(filename, src, new StandardImportResolver(), defaultReport);
	}
	
	public static TableScript FromSource(string filename, string src, IImportResolver ir){
		return FromSource(filename, src, ir, defaultReport);
	}
	
	public static TableScript FromSource(string filename, string src, IImportResolver ir, Action<TabScriptException> report){
		Lexer lex = new Lexer(filename, src);
		lex.OnReport = report;
		TokenList tokenlist = lex.Scan();
		if(tokenlist == null){
			return null;
		}
		
		Parser par = new Parser(tokenlist);
		par.OnReport = report;
		ResolvedImport parsed = par.Parse();
		if(parsed == null){
			return null;
		}
		
		Resolver res = new Resolver(ir);
		res.OnReport = report;
		ResolvedScript resolved = res.Resolve(parsed);
		
		Binder bin = new Binder(resolved);
		bin.OnReport = report;
		TableScript binded = bin.Bind();
		
		Optimizer opt = new Optimizer(binded);
		TableScript runnable = opt.Optimize();
		runnable.OnReport = report;
		
		return runnable;
	}
	
	public static ResolvedImport SourceAsImport(string filename, string src){
		return SourceAsImport(filename, src, defaultReport);
	}
	
	public static ResolvedImport SourceAsImport(string filename, string src, Action<TabScriptException> report){
		Lexer lex = new Lexer(filename, src);
		lex.OnReport = report;
		TokenList tokenlist = lex.Scan();
		if(tokenlist == null){
			return null;
		}
		
		Parser par = new Parser(tokenlist);
		par.OnReport = report;
		ResolvedImport parsed = par.Parse();
		
		return parsed;
	}
}