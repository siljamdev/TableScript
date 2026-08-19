using System;

namespace TableScript;

class Scope : IScope{	
	IScope parent;
	Allocator allocator;
	public string import{get; private init;}
	
	Dictionary<string, int> vars = new();
	
	public Scope(IScope p, Allocator a, string i){
		parent = p;
		allocator = a;
		import = i;
	}
	
	public int define(string filename, int line, string callingImport, string id, bool export, int programCounter){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(vars.ContainsKey(id)){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Variable re-definition: " + import + "::" + id);
		}
		
		Variable v = getVariable(programCounter);
		int uid = allocator.allocate(v);
		vars[id] = uid;
		
		return uid;
	}
	
	public int assign(string filename, int line, string callingImport, string id, string im, int programCounter){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(im == null && vars.TryGetValue(id, out int uid)){
			allocator.variables[uid].setDeath(programCounter);
			return uid;
		}else{
			return parent.assign(filename, line, callingImport, id, im, programCounter);
		}
	}
	
	public int get(string filename, int line, string callingImport, string id, string im, int programCounter){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(im == null && vars.TryGetValue(id, out int uid)){
			allocator.variables[uid].setDeath(programCounter);
			return uid;
		}else{
			return parent.get(filename, line, callingImport, id, im, programCounter);
		}
	}
	
	public IScope endOfLife(){
		return parent;
	}
	
	public Variable getVariable(int programCounter){
		return parent.getVariable(programCounter);
	}
}