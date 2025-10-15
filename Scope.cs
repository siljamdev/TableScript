using System;

namespace TabScript;

class Scope{
	public Scope parent{get; private set;}
	
	List<string> vars = new();
	
	public Scope(Scope p){
		parent = p;
	}
	
	public (int, int) define(int line, string id){
		if(vars.Contains(id)){
			throw new TabScriptException(TabScriptErrorType.Checker, line, "Variable re-definition: " + id);
		}
		
		vars.Add(id);
		
		return (0, vars.Count - 1);
	}
	
	public (int, int) assign(int line, string id, int depth = 0){
		if(vars.Contains(id)){
			return (depth, vars.IndexOf(id));
		}else if(parent != null){
			return parent.assign(line, id, depth + 1);
		}else{
			throw new TabScriptException(TabScriptErrorType.Checker, line, "Undefined variable assignment: " + id);
		}
	}
	
	public (int, int) get(int line, string id, int depth = 0){
		if(vars.Contains(id)){
			return (depth, vars.IndexOf(id));
		}else if(parent != null){
			return parent.get(line, id, depth + 1);
		}else{
			throw new TabScriptException(TabScriptErrorType.Checker, line, "Undefined variable access: " + id);
		}
	}
}