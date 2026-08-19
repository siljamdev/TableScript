using System;

namespace TableScript;

/// <summary>
/// Main class that represents a ready to run script and manages most of the public API
/// </summary>
public class Script{
	internal CFGNode body;
	public BoundFunc[] functions {get; private init;}
	
	public string filename {get; private init;}
	
	/// <summary>
	/// Action that will be called on error
	/// </summary>
	public Action<TabScriptException> OnReport;
	
	Interpreter i;
	
	internal Script(string fn, CFGNode b, BoundFunc[] funcs, Action<TabScriptException> report = null){
		filename = fn;
		body = b;
		functions = funcs;
		OnReport = report;
		
		i = new Interpreter(this);
	}
	
	/// <summary>
	/// Run the script
	/// </summary>
	public void Run(IEnumerable<string> args){
		Run(new Table(new List<string>(args ?? Enumerable.Empty<string>())));
	}
	
	/// <summary>
	/// Run the script
	/// </summary>
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
		return CFGNode.ToString(body) + "\n\n" + string.Join("\n", functions.Select((h, i) => "func_" + i + ": " + h.ToString()));
	}
	
	//######################################################################
	
	static void defaultReport(TabScriptException tsex){
		Console.Error.WriteLine(tsex.ToShortString());
	}
	
	/// <summary>
	/// Generate from source. Will use a default StandardImportResolver
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static Script FromSource(string filename, string src, Optimizations optimizations = Optimizations.Normal){
		return FromSource(filename, src, new StandardImportResolver(), defaultReport, optimizations);
	}
	
	/// <summary>
	/// Generate from source
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static Script FromSource(string filename, string src, IImportResolver ir, Optimizations optimizations = Optimizations.Normal){
		return FromSource(filename, src, ir, defaultReport, optimizations);
	}
	
	/// <summary>
	/// Generate from source
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static Script FromSource(string filename, string src, IImportResolver ir, Action<TabScriptException> report, Optimizations optimizations = Optimizations.Normal){
		ResolvedImport parsed = SourceAsImport(filename, src, report, optimizations);
		
		Script runnable = FromImport(parsed, ir, report, optimizations);
		
		return runnable;
	}
	
	/// <summary>
	/// Generate from import. Will use a default StandardImportResolver
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static Script FromImport(ResolvedImport import, Optimizations optimizations = Optimizations.Normal){
		return FromImport(import, new StandardImportResolver(), defaultReport, optimizations);
	}
	
	/// <summary>
	/// Generate from import 
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static Script FromImport(ResolvedImport import, IImportResolver ir, Optimizations optimizations = Optimizations.Normal){
		return FromImport(import, ir, defaultReport, optimizations);
	}
	
	/// <summary>
	/// Generate from import. 
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static Script FromImport(ResolvedImport import, IImportResolver ir, Action<TabScriptException> report, Optimizations optimizations = Optimizations.Normal){
		Resolver res = new Resolver(ir);
		res.OnReport = report;
		ResolvedScript resolved = res.Resolve(import);
		
		Binder bin = new Binder(resolved, optimizations);
		bin.OnReport = report;
		BoundScript binded = bin.Bind();
		
		Optimizer opt = new Optimizer(binded, optimizations);
		Script runnable = opt.Optimize();
		runnable.OnReport = report;
		
		return runnable;
	}
	
	/// <summary>
	/// Generate an import from source
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static ResolvedImport SourceAsImport(string filename, string src, Optimizations optimizations = Optimizations.Normal){
		return SourceAsImport(filename, src, defaultReport, optimizations);
	}
	
	/// <summary>
	/// Generate an import from source
	/// </summary>
	/// <exception cref="TableScript.TabScriptException">Thrown when an error occurs while compiling</exception>
	public static ResolvedImport SourceAsImport(string filename, string src, Action<TabScriptException> report, Optimizations optimizations = Optimizations.Normal){
		Lexer lex = new Lexer(filename, src);
		lex.OnReport = report;
		TokenList tokenlist = lex.Scan();
		
		Parser par = new Parser(tokenlist);
		par.OnReport = report;
		ResolvedImport parsed = par.Parse();
		
		if((optimizations & Optimizations.EarlyOptimizations) != 0){
			EarlyOptimizer opt = new EarlyOptimizer(parsed, optimizations);
			parsed = opt.Optimize();
		}
		
		return parsed;
	}
}