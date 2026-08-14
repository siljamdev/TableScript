using System;

namespace TabScript;

class FunctionScope : IScope{	
	IScope parent;
	Allocator allocator;
	public string import{get; private init;}
	public int funcIndex{get; private init;}
	
	Dictionary<string, int> vars = new();
	
	public FunctionScope(IScope p, Allocator a, string i, int fIndex){
		parent = p;
		allocator = a;
		import = i;
		funcIndex = fIndex;
	}
	
	public int define(string filename, int line, string callingImport, string id, bool export, int programCounter){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(vars.ContainsKey(id)){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Variable re-definition: " + import + "::" + id);
		}
		
		Variable v = getVariable();
		vars[id] = allocator.allocate(v);
		
		int op = allocator.getOperationId();
		v.operations[op] = (false, programCounter);
		
		return op;
	}
	
	public int assign(string filename, int line, string callingImport, string id, string im, int programCounter){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(im == null && vars.TryGetValue(id, out int uid)){
			int op = allocator.getOperationId();
			allocator.variables[uid].operations[op] = (false, programCounter);
			return op;
		}else{
			return parent.assign(filename, line, callingImport, id, im, programCounter);
		}
	}
	
	public int get(string filename, int line, string callingImport, string id, string im, int programCounter){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(im == null && vars.TryGetValue(id, out int uid)){
			int op = allocator.getOperationId();
			allocator.variables[uid].operations[op] = (true, programCounter);
			return op;
		}else{
			return parent.get(filename, line, callingImport, id, im, programCounter);
		}
	}
	
	public IScope endOfLife(){
		return parent;
	}
	
	public Variable getVariable(){
		return new Variable(){frame = funcIndex};
	}
}