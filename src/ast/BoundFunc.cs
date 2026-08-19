namespace TableScript;

/// <summary>
/// Function representation
/// </summary>
public abstract class BoundFunc{
	public string import {get; private init;}
	public string identifier {get; private init;}
	public int arity {get; private init;}
	
	internal BoundFunc(string import, string identifier, int arity){
		this.import = import;
		this.identifier = identifier;
		this.arity = arity;
	}
	
	public bool Matches(string callImport, string callIdentifier, int callArity){
		return (callImport == null || callImport == import) && callIdentifier == identifier && callArity == arity;
	}
}

class BoundNativeFunc : BoundFunc{
	public CFGNode body;
	
	public BoundNativeFunc(string import, string identifier, int arity, CFGNode body) : base(import, identifier, arity){
		this.body = body;
	}
	
	public override string ToString(){
		return "function " + import + "::" + identifier + "(" + arity + " parameters){\n" + CFGNode.ToString(body) + "\n}";
	}
}

class BoundExternFunc : BoundFunc{
	public Func<Table[], Table> body {get; private init;}
	
	public BoundExternFunc(string import, string identifier, int arity, Func<Table[], Table> body) : base(import, identifier, arity){
		this.body = body;
	}
	
	public override string ToString(){
		return "function " + import + "::" + identifier + "(" + arity + " parameters){ EXTERN; }";
	}
}