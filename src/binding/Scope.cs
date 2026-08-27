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
	
	public int define(string filename, int line, string callingImport, string id, bool export){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(vars.ContainsKey(id)){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Variable re-definition: " + import + "::" + id);
		}
		
		Variable v = getVariable();
		int uid = allocator.allocate(v);
		vars[id] = uid;
		
		return uid;
	}
	
	public int assign(string filename, int line, string callingImport, string id, string im){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(im == null && vars.TryGetValue(id, out int uid)){
			return uid;
		}else{
			return parent.assign(filename, line, callingImport, id, im);
		}
	}
	
	public int get(string filename, int line, string callingImport, string id, string im){
		if(callingImport != import){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unexpected scope access from foreign import: " + callingImport);
		}
		
		if(im == null && vars.TryGetValue(id, out int uid)){
			return uid;
		}else{
			return parent.get(filename, line, callingImport, id, im);
		}
	}
	
	public IScope endOfLife(){
		return parent;
	}
	
	public Variable getVariable(){
		return parent.getVariable();
	}
}