using System;

namespace TabScript;

class Resolver{
	IImportResolver impres;
	
	public Resolver(IImportResolver res){
		impres = res;
	}
	
	public TableScript Resolve(Stmt[] stms){
		List<string> toImport = new();
		List<Stmt> bod = new();
		
		List<TabFunc> fs = new();
		
		for(int i = 0; i < stms.Length; i++){
			switch(stms[i]){
				case ImportStmt a1:
					toImport.Add(a1.import);
				break;
				
				case FunctionStmt a2:
					fs.Add(a2.ToTabFunc("local"));
				break;
				
				default:
					bod.Add(stms[i]);
				break;
			}
		}
		
		HashSet<string> imported = new(toImport.Count);
		
		for(int i = 0; i < toImport.Count; i++){
			if(imported.Contains(toImport[i])){
				continue;
			}
			
			ResolvedImport rim = impres.Resolve(toImport[i]);
			
			toImport.AddRange(rim.furtherImports);
			
			fs.AddRange(rim.functions.Select(h => h.ToTabFunc(toImport[i])));
			
			imported.Add(toImport[i]);
		}
		
		return new TableScript(bod.ToArray(), fs.ToArray());
	}
}

public interface IImportResolver{
	public ResolvedImport Resolve(string import);
}

public record ResolvedImport(string[] furtherImports, FunctionStmt[] functions);

public class StandardImportResolver : IImportResolver{
	static void report(TabScriptException x){
		Console.Error.WriteLine(x.ToShortString());
	}
	
	public Action<TabScriptException> OnReport = report;
	
	public ResolvedImport Resolve(string import){
		switch(import){
			case "stdlib":
				return new ResolvedImport(new string[0], StdLib.All);
			
			case "stdnum":
				return new ResolvedImport(new string[0], StdNum.All);
			
			default:
				OnReport(new TabScriptException(TabScriptErrorType.Resolver, -1, "Unable to resolve import: " + import));
			return new ResolvedImport(new string[0], new FunctionStmt[0]);
		}
	}
}