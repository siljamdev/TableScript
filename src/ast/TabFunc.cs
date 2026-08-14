namespace TabScript;

public abstract class TabFunc{
	public string import {get; private init;}
	public string identifier {get; private init;}
	public string[] pars {get; private init;}
	public bool self {get; private init;}
	public bool export {get; private init;}
	public string filename {get; private init;}
	public int line {get; private init;}
	
	public int arity => pars.Length;
	
	internal TabFunc(string import, string identifier, string[] pars, bool self, bool export, string filename, int line){
		this.import = import;
		this.identifier = identifier;
		this.pars = pars;
		this.self = self;
		this.export = export;
		this.filename = filename;
		this.line = line;
	}
	
	public bool Matches(string callImport, string callIdentifier, int callArity){
		return (callImport == null || callImport == import) && callIdentifier == identifier && callArity == arity;
	}
	
	public bool SameSignature(TabFunc other){
		return import == other.import && identifier == other.identifier && arity == other.arity;
	}
}

class TabNativeFunc : TabFunc{
	public BlockStmt body;
	
	public TabNativeFunc(string import, string identifier, string[] pars, bool self, bool export, BlockStmt body, string filename, int line) : base(import, identifier, pars, self, export, filename, line){
		this.body = body;
	}
	
	public override string ToString(){
		return (export ? "export " : "") + "function " + import + "::" + identifier + "(" + string.Join(", ", pars) + ")" + body.ToBlockString();
	}
}

class TabExternFunc : TabFunc{
	public Func<Table[], Table> body {get; private init;}
	public string description {get; private init;}
	
	public TabExternFunc(string import, string identifier, string[] pars, bool self, bool export, Func<Table[], Table> body, string description, string filename, int line) : base(import, identifier, pars, self, export, filename, line){
		this.body = body;
		this.description = description;
	}
	
	public override string ToString(){
		return (export ? "export " : "") + "function " + import + "::" + identifier + "(" + string.Join(", ", pars) + "){ EXTERN; }" + (description == null ? "" : (" //" + description));
	}
}