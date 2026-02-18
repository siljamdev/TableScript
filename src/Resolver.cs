using System;
using System.Text;

namespace TabScript;

class Resolver{
	IImportResolver impres;
	
	public Action<TabScriptException> OnReport {get{
		return impres.OnReport;
	}set{
		impres.OnReport = value;
	}}
	
	public Resolver(IImportResolver res){
		impres = res;
	}
	
	public ResolvedScript Resolve(ResolvedImport parsed){
		List<Snippet> snippets = new();
		Dictionary<string, string[]> availableImports = new(); //symbols
		List<TabFunc> fs = new(); //All Functions
		
		List<(string toImp, string filename)> toImport = new(); //Full imports will be used here
		
		string mainImportFull = validFullImport(parsed.filename);
		string mainImport = validImportName(mainImportFull);
		
		Snippet main = new Snippet(parsed.filename, mainImport, parsed.body ?? Array.Empty<Stmt>());
		
		fs.AddRange(parsed.functions.Select(s => s.ToTabFunc(mainImport, parsed.filename)));
		
		toImport.AddRange(parsed.imports.Select(im => (im, parsed.filename)).ToArray());
		availableImports[mainImport] = parsed.imports.Select(im => validImportName(im)).ToArray();
		
		HashSet<string> imported = new(toImport.Count + 1); //Avoid duplicates, here full imports will be used
		imported.Add(mainImportFull);
		
		//Process imports
		for(int i = 0; i < toImport.Count; i++){
			if(imported.Contains(toImport[i].toImp)){ //Avoid duplicates
				continue;
			}
			
			string currImportFull = validFullImport(toImport[i].toImp);
			string currImport = validImportName(currImportFull);
			
			ResolvedImport rim = impres.Resolve(currImportFull, toImport[i].filename);
			
			Stmt[] varDecls = rim.body == null ? Array.Empty<Stmt>() : rim.body.Where(s => s is VarDeclStmt).ToArray(); //Only variable declarations
			if(varDecls.Length > 0){
				Snippet curr = new Snippet(rim.filename, currImport, varDecls);
				snippets.Add(curr);
			}
			
			if(rim.functions != null){
				fs.AddRange(rim.functions.Select(s => s.ToTabFunc(currImport, rim.filename)));
			}
			
			if(rim.imports != null){ 
				toImport.AddRange(rim.imports.Select(im => (im, rim.filename)).ToArray()); //Further imports
				availableImports[currImport] = rim.imports.Select(im => validImportName(im)).ToArray(); //Symbols
			}else{
				availableImports[currImport] = Array.Empty<string>(); //Symbols
			}
			
			imported.Add(currImportFull);
		}
		
		snippets.Reverse(); //The ones sued at the end are the first ones
		
		return new ResolvedScript(main, snippets.ToArray(), fs.ToArray(), availableImports);
	}
	
	static string validFullImport(string i){
		try{
			//This throws if the path is invalid
			string fileName = Path.GetFileName(Path.GetFullPath(i));
			
			return string.IsNullOrEmpty(fileName) ? i : fileName;
		}catch{
			return i;
		}
	}
	
	static string validImportName(string i){
		StringBuilder sb = new();
		
		foreach(char c in i){
			if(char.IsLetterOrDigit(c)){
				sb.Append(c);
			}else{
				sb.Append("_");
			}
		}
		
		return sb.ToString();
	}
}

public record ResolvedImport(string filename, string[] imports, Stmt[] body, FunctionStmt[] functions){
	public override string ToString(){
		return filename + (body != null ? ("\n" + string.Join("\n", body.Select(s => s.ToString()))) : "") + (functions != null ? ("\n" + string.Join("\n", functions.Select(f => f.ToString()))) : "");
	}
}

public record Snippet(string filename, string import, Stmt[] body){
	public override string ToString(){
		return filename + ", " + import + (body != null ? ("\n" + string.Join("\n", body.Select(s => s.ToString()))) : "");
	}
}

record ResolvedScript(Snippet mainBody, Snippet[] bodies, TabFunc[] allFunctions, Dictionary<string, string[]> availableImports){
	public override string ToString(){
		return mainBody.ToString() + "\n" + string.Join("\n", bodies.Select(s => s.ToString())) + "\n" + string.Join("\n", allFunctions.Select(f => f.ToString()));
	}
}