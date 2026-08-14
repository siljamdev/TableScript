using System;
using System.Text;

namespace TabScript;

class Allocator{
	public List<Variable> variables {get; private init;} = new();
	int operationIdCounter = 0;
	
	bool reuseSlots;
	
	Dictionary<int, Variable> operationVariables;
	Dictionary<int, Variable[]> frameVars;
	Variable[] localVars;
	
	//Returns uid
	public int allocate(Variable va){
		int v = variables.Count;
		va.uid = v;
		variables.Add(va);
		
		return v;
	}
	
	public int getOperationId(){
		return operationIdCounter++;
	}
	
	public void startIndexing(Optimizations opt){
		//variables.ForEach(v => Console.WriteLine(v));
		
		reuseSlots = (opt & Optimizations.VariableIndexReusing) != 0;
		
		frameVars = variables.GroupBy(v => v.frame).ToDictionary(g => g.Key, g => g.ToArray());
		
		operationVariables = variables.SelectMany(v => v.operations.Keys.Select(k => (k, v))).ToDictionary(x => x.k, x => x.v);
	}
	
	public int getIndex(int op){
		Variable v = operationVariables[op];
		int? sav = v.index;
		if(sav != null){
			return (int) sav;
		}
		
		setIndexes(v.frame);
		
		return (int) v.index;
	}
	
	void setIndexes(int frame){
		Variable[] vars = frameVars[frame];
		
		List<Variable> alive = new();
		
		for(int i = 0; i < vars.Length; i++){
			int chosen = alive.Count;
			
			if(reuseSlots){
				for(int j = 0; j < alive.Count; j++){
					if(alive[j].death < vars[i].born){
						alive[j] = vars[i];
						chosen = j;
						break;
					}
				}
				
				if(chosen == alive.Count){
					alive.Add(vars[i]);
				}
			}else{
				alive.Add(vars[i]);
			}
			
			vars[i].index = frame == -1 ? (-chosen - 1) : chosen;
			
			//Console.WriteLine("Chosen index " + vars[i].index + " for UID " + vars[i].uid);
		}
	}
}

class Variable{
	public int frame; //-1 is global / main. else its func index
	public bool isRelative => frame != -1;
	public int uid;
	
	public Dictionary<int, (bool isAccess, int programCounter)> operations = new(); //false is set, true is get
	
	IEnumerable<KeyValuePair<int, (bool isAccess, int programCounter)>> assignments => operations.Where(kvp => !kvp.Value.isAccess);
	public int firstAssign => assignments.Count() == 0 ? -1 : assignments.Min(kvp => kvp.Value.programCounter);
	public int lastAssign => assignments.Count() == 0 ? -1 : assignments.Max(kvp => kvp.Value.programCounter);
	
	IEnumerable<KeyValuePair<int, (bool isAccess, int programCounter)>> accesses => operations.Where(kvp => kvp.Value.isAccess);
	public int firstAccess => accesses.Count() == 0 ? -1 : accesses.Min(kvp => kvp.Value.programCounter);
	public int lastAccess => accesses.Count() == 0 ? -1 : accesses.Max(kvp => kvp.Value.programCounter);
	
	public int death => Math.Max(lastAccess, lastAssign);
	public int born => firstAssign;
	
	public int? index = null;
	
	public override string ToString(){
		StringBuilder sb = new();
		sb.AppendLine("UID: " + uid + ", born: " + born + " death: " + death + ", operations:");
		
		foreach(var kvp in operations){
			sb.AppendLine("    OP: " + kvp.Key + ", isAccess: " + kvp.Value.isAccess + ", PC: " + kvp.Value.programCounter);
		}
		
		return sb.ToString();
	}
}