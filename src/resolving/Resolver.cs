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
		Dictionary<string, Dictionary<string, string>> availableImports = new(); //symbols: Dictionary of imports available on an import, having both key (how its called) and value (what the real import name is)
		List<TabFunc> fs = new(); //All Functions
		
		Queue<(string filename, string import, ImportStmt stmt)> pending = new(); //References will be used here
		
		string mainImport = validImportName(parsed.filename);
		
		Snippet main = new Snippet(parsed.filename, mainImport, parsed.body ?? Array.Empty<Stmt>());
		
		if(parsed.globals != null && parsed.globals.Length > 0){ //Add another snippet for globals
			snippets.Add(new Snippet(parsed.filename, mainImport, parsed.globals));
		}
		
		fs.AddRange(parsed.functions.Select(s => s.ToTabFunc(mainImport, parsed.filename)));
		
		foreach(ImportStmt im in parsed.imports){
			pending.Enqueue((parsed.filename, mainImport, im));
		}
		availableImports[mainImport] = new Dictionary<string, string>();
		
		HashSet<string> imported = new(pending.Count + 1); //Avoid duplicates, here import names will be used
		HashSet<string> importedFilenames = new(pending.Count + 1); //Avoid duplicates, here import names will be used
		imported.Add(mainImport);
		importedFilenames.Add(parsed.filename);
		
		//Process imports
		while(pending.Count > 0){
			(string fromFilename, string fromImport, ImportStmt s) = pending.Dequeue();
			string reference = s.reference;
			
			ResolvedImport rim = impres.Resolve(reference, fromFilename);
			
			if(rim == null){ //Impossible to resolve
				continue;
			}
			
			string import = validImportName(rim.filename);
			
			if(imported.Add(import)){ //Avoid duplicates
				importedFilenames.Add(rim.filename);
				
				if(rim.globals != null && rim.globals.Length > 0){ //Add new snippet for globals
					snippets.Add(new Snippet(rim.filename, import, rim.globals));
				}
				
				if(rim.functions != null){ //Add functions
					fs.AddRange(rim.functions.Select(s => s.ToTabFunc(import, rim.filename)));
				}
				
				availableImports[import] = new Dictionary<string, string>(); //Init for this import
				
				if(rim.imports != null){
					foreach(ImportStmt im in rim.imports){
						pending.Enqueue((rim.filename, import, im));
					}
				}
			}else if(!importedFilenames.Contains(rim.filename)){ //Same import name, diff file
				OnReport?.Invoke(new TabScriptException(TabScriptErrorType.Resolver, fromFilename, s.line, "Different filenames for imports, but same import name: '" + import + "'"));
				continue;
			}
			
			availableImports[fromImport][import] = import; //Add this import to the one who called it
		}
		
		snippets.Reverse(); //The ones added at the end are the first ones
		
		return new ResolvedScript(main, snippets.ToArray(), fs.ToArray(), availableImports);
	}
	
	static string validImportName(string fn){
		try{
			//This throws if the path is invalid
			string filename = Path.GetFileNameWithoutExtension(fn);
			
			fn = string.IsNullOrEmpty(filename) ? fn : filename;
		}catch{}
		
		StringBuilder sb = new();
		
		foreach(char c in fn){
			if(char.IsLetterOrDigit(c)){
				sb.Append(c);
			}else{
				sb.Append("_");
			}
		}
		
		return sb.ToString();
	}
}

record Snippet(string filename, string import, Stmt[] body){
	public override string ToString(){
		return filename + ", '" + import + "':" +
			(body != null ? ("\n" + string.Join("\n", body.Select(s => s.ToString()))) : "");
	}
}

record ResolvedScript(Snippet mainBody, Snippet[] bodies, TabFunc[] allFunctions, Dictionary<string, Dictionary<string, string>> availableImports){
	public override string ToString(){
		return string.Join("\n", bodies.Select(s => s.ToString())) + 
			"\n" + mainBody.ToString() +
			"\n" + string.Join("\n", allFunctions.Select(f => f.ToString()));
	}
}