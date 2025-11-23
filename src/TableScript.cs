using System;

namespace TabScript;

public class TableScript{
	public Stmt[] topLevel {get; private set;}
	public TabFunc[] functions {get; private set;}
	
	internal TableScript(Stmt[] top, TabFunc[] funcs, Action<TabScriptException> rep = null){
		topLevel = top;
		functions = funcs;
		OnReport = rep ?? repor;
	}
	
	static void repor(TabScriptException x){
		Console.Error.WriteLine(x.ToShortString());
	}
	
	public Action<TabScriptException> OnReport = repor;
	
	public static TableScript FromSource(string src){
		return FromSource(src, new StandardImportResolver(), repor);
	}
	
	public static TableScript FromSource(string src, IImportResolver ir, Action<TabScriptException> report){
		Lexer l = new Lexer(src);
		l.OnReport = report;
		
		Token[] tl = l.Scan();
		
		Parser p = new Parser(tl);
		p.OnReport = report;
		
		Stmt[] s = p.Parse();
		
		if(s == null){
			return null;
		}
		
		Resolver rr = new Resolver(ir);
		
		TableScript x = rr.Resolve(s);
		
		Checker c = new Checker();
		
		c.OnReport = report;
		
		x = c.Check(x);
		
		Optimizer o = new Optimizer(x);
		
		x = o.Optimize();
		
		x.OnReport = report;
		
		return x;
	}
	
	public void Run(Table args){
		Interpreter i = new Interpreter();
		
		try{
			i.Interpret(this, args);
		}catch(TabScriptException ex){
			OnReport(ex);
		}
	}
	
	public override string ToString(){
		return string.Join("\n", topLevel.Select(h => h.ToString())) + "\n" + string.Join("\n", functions.Select(h => h.ToString()));
	}
}