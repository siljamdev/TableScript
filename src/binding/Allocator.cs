using System;
using System.Text;

namespace TableScript;

class Allocator{
	public List<Variable> variables {get; private init;} = new();
	
	bool reuseSlots;
	
	Dictionary<int, Variable[]> frameVars;
	
	//Returns uid
	public int allocate(Variable va){
		int v = variables.Count;
		//va.uid = v;
		variables.Add(va);
		
		return v;
	}
	
	public void startIndexing(Optimizations opt){
		//Console.WriteLine(this);
		
		reuseSlots = (opt & Optimizations.VariableIndexReusing) != 0;
		
		frameVars = variables.GroupBy(v => v.frame).ToDictionary(g => g.Key, g => g.ToArray());
	}
	
	public int getIndex(int uid){
		Variable v = variables[uid];
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
	
	public override string ToString(){
		StringBuilder sb = new();
		for(int uid = 0; uid < variables.Count; uid++){
			sb.Append("UID: " + uid);
			sb.AppendLine(variables[uid].ToString());
		}
		
		return sb.ToString();
	}
}

class Variable{
	public int frame; //-1 is global / main. else its func index
	public bool isRelative => frame != -1;
	
	public int death;
	public int born;
	
	public int? index = null;
	
	public Variable(int f, int b){
		frame = f;
		born = b;
		death = b;
	}
	
	public void setDeath(int d){
		death = Math.Max(death, d);
	}
	
	public override string ToString(){
		return ", born: " + born + " death: " + death;
	}
}