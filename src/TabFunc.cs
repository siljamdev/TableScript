namespace TabScript;

public abstract record TabFunc(string import, string identifier, string[] pars, bool self, int line){
	public int arity => pars.Length;
}

record TabNativeFunc(string import, string identifier, string[] pars, bool self, BlockStmt body, int line) : TabFunc(import, identifier, pars, self, line){
	public override string ToString(){
		return "function " + import + "::" + identifier + "(" + string.Join(", ", pars) + ")" + body;
	}
}

record TabExternFunc(string import, string identifier, string[] pars, bool self, Func<Table[], Table> body, int line) : TabFunc(import, identifier, pars, self, line){
	public override string ToString(){
		return "function " + import + "::" + identifier + "(" + string.Join(", ", pars) + "){ ##NATIVE##; }";
	}
}