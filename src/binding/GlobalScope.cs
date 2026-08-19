using System;

namespace TableScript;

class GlobalScope : IScope{
	Allocator allocator;
	
	Dictionary<(string identifier, string import), (int uid, bool export)> vars = new();
	
	public GlobalScope(Allocator a){
		allocator = a;
	}
	
	public int define(string filename, int line, string callingImport, string id, bool export, int programCounter){
		if(vars.Keys.Any(t => t.import == callingImport && t.identifier == id)){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Variable re-definition: " + callingImport + "::" + id);
		}
		
		Variable v = getVariable(programCounter);
		int uid = allocator.allocate(v);
		vars[(id, callingImport)] = (uid, export);
		
		return uid;
	}
	
	int findUidAssign(string filename, int line, string callingImport, string id, string im){
		int? uid = null;
		string import = null;
		
		if(im == null && vars.TryGetValue((id, callingImport), out (int uid, bool export) temp)){ //Match local first
			uid = temp.uid;
			import = callingImport;
		}
		
		if(uid == null){
			foreach(KeyValuePair<(string identifier, string import), (int uid, bool export)> kvp in vars){
				if(kvp.Key.identifier == id && (im == null || im == kvp.Key.import) && (kvp.Key.import == callingImport || kvp.Value.export)){
					uid = kvp.Value.uid;
					import = kvp.Key.import;
				}
			}
		}
		
		if(uid == null){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Undefined variable assignment: " + (im == null ? "" : (im + "::")) + id);
		}
		
		if(import != callingImport){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unauthorized variable assignment from foreign import: " + import + "::" + id);
		}
		
		return (int) uid;
	}
	
	public int assign(string filename, int line, string callingImport, string id, string im, int programCounter){
		int? uid = null;
		string import = null;
		
		if(im == null && vars.TryGetValue((id, callingImport), out (int uid, bool export) temp)){ //Match local first
			uid = temp.uid;
			import = callingImport;
		}
		
		if(uid == null){
			foreach(KeyValuePair<(string identifier, string import), (int uid, bool export)> kvp in vars){
				if(kvp.Key.identifier == id && (im == null || im == kvp.Key.import) && (kvp.Key.import == callingImport || kvp.Value.export)){
					uid = kvp.Value.uid;
					import = kvp.Key.import;
				}
			}
		}
		
		if(uid == null){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Undefined variable assignment: " + (im == null ? "" : (im + "::")) + id);
		}
		
		if(import != callingImport){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Unauthorized variable assignment from foreign import: " + import + "::" + id);
		}
		
		allocator.variables[(int) uid].setDeath(programCounter);
		return (int) uid;
	}
	
	public int get(string filename, int line, string callingImport, string id, string im, int programCounter){
		int? uid = null;
		
		if(im == null && vars.TryGetValue((id, callingImport), out (int uid, bool export) temp)){ //Match local first
			uid = temp.uid;
		}
		
		if(uid == null){
			foreach(KeyValuePair<(string identifier, string import), (int uid, bool export)> kvp in vars){
				if(kvp.Key.identifier == id && (im == null || im == kvp.Key.import) && (kvp.Key.import == callingImport || kvp.Value.export)){
					uid = kvp.Value.uid;
				}
			}
		}
		
		if(uid == null){
			throw new TabScriptException(TabScriptErrorType.Binder, filename, line, "Undefined variable access: " + (im == null ? "" : (im + "::")) + id);
		}
		
		allocator.variables[(int) uid].setDeath(programCounter);
		return (int) uid;
	}
	
	public IScope endOfLife(){
		return null;
	}
	
	public Variable getVariable(int programCounter){
		return new Variable(-1, programCounter);
	}
}