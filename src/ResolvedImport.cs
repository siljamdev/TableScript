namespace TableScript;

public class ResolvedImport{
	public string filename {get; private init;}
	internal ImportStmt[] imports;
	internal GlobalDeclStmt[] globals;
	internal Stmt[] body;
	internal FunctionStmt[] funcs;
	
	public string[] furtherImports => imports?.Select(i => i.reference).ToArray() ?? Array.Empty<string>();
	public string[] definedGlobals => globals?.Where(g => g.export).Select(g => g.identifier).ToArray() ?? Array.Empty<string>();
	public FunctionStmt[] functions => funcs.ToArray();
	
	internal ResolvedImport(string fn, ImportStmt[] i, GlobalDeclStmt[] g, Stmt[] b, FunctionStmt[] f){
		filename = fn;
		imports = i;
		globals = g;
		body = b;
		funcs = f;
	}
	
	public ResolvedImport(string fn, string[] furtherImports, Dictionary<string, Table> definedGlobals, FunctionStmt[] funcs)
		: this(fn, furtherImports?.Select(i => new ImportStmt(i, -1)).ToArray(), definedGlobals?.Select(kvp => new GlobalDeclStmt(kvp.Key, true, new LiteralExpr(kvp.Value), -1)).ToArray(), null, funcs){
			
	}
	
	public override string ToString(){
		return (imports != null ? (string.Join("\n", imports.Select(i => i.ToString()))) : "") + 
			(globals != null ? ("\n\n" + string.Join("\n", globals.Select(g => g.ToString()))) : "") +
			(body != null ? ("\n\n" + string.Join("\n", body.Select(s => s.ToString()))) : "") + 
			(funcs != null ? ("\n\n" + string.Join("\n", funcs.Select(f => f.ToString()))) : "");
	}
	
	public string ToCompactString(){
		return (imports != null ? (string.Join("\n", imports.Select(i => i.ToCompactString()))) : "") +
		(globals != null ? ("\n" + string.Join("\n", globals.Select(g => g.ToCompactString()))) : "") +
		(body != null ? ("\n" + string.Join("\n", body.Select(s => s.ToCompactString()))) : "") +
		(funcs != null ? ("\n" + string.Join("\n", funcs.Select(f => f.ToCompactString()))) : "");
	}
}