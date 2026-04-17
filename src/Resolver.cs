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
		
		Snippet main = parsed.GetAsSnippet(mainImport, false);
		
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
			
			Snippet curr = rim.GetAsSnippet(currImport, true);
			if(curr != null){
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
		return filename + ":" + (imports != null ? ("\n" + string.Join("\n", imports.Select(i => "import \"" + i + "\";"))) : "") + (body != null ? ("\n\n" + string.Join("\n", body.Select(s => s.ToString()))) : "") + (functions != null ? ("\n\n" + string.Join("\n", functions.Select(f => f.ToString()))) : "");
	}
	
	string _compactStr(){
		return (imports != null ? (string.Join("\n", imports.Select(i => "import \"" + i + "\";"))) : "") + (body != null ? ("\n" + string.Join("\n", body.Select(s => s.ToCompactString()))) : "") + (functions != null ? ("\n" + string.Join("\n", functions.Select(f => f.ToCompactString()))) : "");
	}
	
	public string ToCompactString(){
		Optimizer opt = new Optimizer(this);
		return opt.OptimizeImport()._compactStr();
	}
	
	public Snippet GetAsSnippet(string import, bool onlyVars){
		if(onlyVars){
			Stmt[] varDecls = body == null ? Array.Empty<Stmt>() : body.Where(s => s is VarDeclStmt).ToArray(); //Only variable declarations
			if(varDecls.Length > 0){
				return new Snippet(filename, import, varDecls);
			}else{
				return null;
			}
		}else{
			return new Snippet(filename, import, body ?? Array.Empty<Stmt>());
		}
	}
}

public record Snippet(string filename, string import, Stmt[] body){
	public override string ToString(){
		return filename + ", \"" + import + "\":" + (body != null ? ("\n" + string.Join("\n", body.Select(s => s.ToString()))) : "");
	}
}

record ResolvedScript(Snippet mainBody, Snippet[] bodies, TabFunc[] allFunctions, Dictionary<string, string[]> availableImports){
	public override string ToString(){
		return mainBody.ToString() + "\n" + string.Join("\n", bodies.Select(s => s.ToString())) + "\n" + string.Join("\n", allFunctions.Select(f => f.ToString()));
	}
}