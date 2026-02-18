namespace TabScript;

public abstract record TabFunc(string import, string identifier, string[] pars, bool self, bool export, string filename, int line){
	public int arity => pars.Length;
	
	public bool Matches(string callImport, string callIdentifier, int callArity){
		return (callImport == null || callImport == import) && callIdentifier == identifier && callArity == arity;
	}
	
	public bool SameSignature(TabFunc other){
		return import == other.import && identifier == other.identifier && arity == other.arity;
	}
}

record TabNativeFunc(string import, string identifier, string[] pars, bool self, bool export, BlockStmt body, string filename, int line) : TabFunc(import, identifier, pars, self, export, filename, line){
	public override string ToString(){
		return (export ? "export " : "") + "function " + import + "::" + identifier + "(" + string.Join(", ", pars) + ")" + body;
	}
}

record TabExternFunc(string import, string identifier, string[] pars, bool self, bool export, Func<Table[], Table> body, string description, string filename, int line) : TabFunc(import, identifier, pars, self, export, filename, line){
	public override string ToString(){
		return (export ? "export " : "") + "function " + import + "::" + identifier + "(" + string.Join(", ", pars) + "){ EXTERN; }" + (description == null ? "" : (" //" + description));
	}
}