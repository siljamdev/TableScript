using System;
using System.Text;

namespace TabScript;

public class Table{
	static Random rand = new();
	
	static readonly List<string> empty = new List<string>();
	
	public static Table True => new Table(1);
	
	public static Table False => new Table(0);
	
	List<string> tab;
	
	bool isSpecial;
	int specialLen;
	
	public bool IsNumber => isSpecial;
	
	public int Length => isSpecial ? specialLen : tab.Count;
	
	public bool Truthy => Length > 0;
	
	public IReadOnlyList<string> contents => isSpecial ? Enumerable.Repeat("", Math.Max(0, specialLen)).ToList().AsReadOnly() : tab.AsReadOnly();
	
	public string this[int index]{get{
		return isSpecial ? "" : tab[index];
	}
	set{
		makeNormal();
		tab[index] = value;
	}}
	
	public Table(){
		tab = new List<string>();
	}
	
	public Table(params string[] ss){
		tab = new List<string>(ss);
	}
	
	public Table(Table t){
		if(t.isSpecial){
			isSpecial = true;
			specialLen = t.specialLen;
			return;
		}
		
		List<string> u = t.tab;
		u ??= empty;
		tab = new List<string>(u);
	}
	
	public Table(int n){
		isSpecial = true;
		specialLen = n;
	}
	
	public Table(List<string> t){
		t ??= empty;
		tab = new List<string>(t);
	}
	
	public Table Clone(){
		return new Table(this);
	}
	
	void makeNormal(){
		if(!isSpecial){
			return;
		}
		
		specialLen = Math.Max(0, specialLen);
		
		tab = Enumerable.Repeat("", specialLen).ToList();
		
		isSpecial = false;
		specialLen = 0;
	}
	
	public void SetElem(TabIndex ind, Table t){
		makeNormal();
		
		switch(ind.mode){
			case TabIndexMode.Number:
				int real;
				if(ind.num < 0){
					real = Length + ind.num;
				}else{
					real = ind.num;
				}
				
				if(real >= 0 && real < Length){
					this[real] = t.AsString();
				}else if(real >= Length){
					tab.AddRange(Enumerable.Repeat("", real - Length));
					tab.Add(t.AsString());
				}
			break;
			
			case TabIndexMode.Random:
				if(Length == 0){
					tab.Add(t.AsString());
					return;
				}
				
				this[rand.Next(Length)] = t.AsString();
			break;
		}
	}
	
	public Table GetElem(TabIndex ind){
		switch(ind.mode){
			case TabIndexMode.Number:
				int real;
				if(ind.num < 0){
					real = Length + ind.num;
				}else{
					real = ind.num;
				}
				
				if(real >= 0 && real < Length){
					return new Table(this[real]);
				}else{
					return new Table();
				}
			break;
			
			case TabIndexMode.Random:
				if(Length == 0){
					return new Table();
				}
				
				if(isSpecial){
					return new Table("");
				}
				
				return new Table(this[rand.Next(Length)]);
			break;
			
			case TabIndexMode.Length:
				return new Table(Length);
			break;
			
			default:
				return new Table();
			break;
		}
	}
	
	public Table GetRange(TabIndex ind, TabIndex len){
		int start;
		int length;
		
		//Exit early
		if(Length == 0){
			return new Table();
		}
		
		switch(ind.mode){
			case TabIndexMode.Number:
				if(ind.num < 0){
					start = Length + ind.num;
				}else{
					start = ind.num;
				}
			break;
			
			case TabIndexMode.Random:
				start = rand.Next(Length);
			break;
			
			default:
				start = 0;
			break;
		}
		
		switch(len.mode){
			case TabIndexMode.Number:
				length = len.num;
			break;
			
			case TabIndexMode.Length:
				length = tab.Count;
			break;
			
			default:
				length = 0;
			break;
		}
		
		//Handle negative length
		if(length < 0){
			start += length;
			length = -length;
		}
		
		//Completely out
		if(start < 0 && start + length < 0){
			return new Table();
		}
		
		//Shorten
		if(start < 0){
			length += start;
			start = 0;
		}
		
		if(start >= Length){
			return new Table();
		}
		
		//Shorten
		if(start + length > Length){
			length = Length - start;
		}
		
		if(isSpecial){
			return new Table(length);
		}
		
		return new Table(tab.GetRange(start, length));
	}
	
	public void AddRange(Table t){
		if(isSpecial && t.isSpecial){
			specialLen += t.specialLen;
			return;
		}
		
		makeNormal();
		
		if(t.isSpecial){
			tab.AddRange(Enumerable.Repeat("", t.Length));
		}else{
			tab.AddRange(t.tab);
		}
	}
	
	public void RemoveAll(Table t){
		if(isSpecial && t.isSpecial){
			specialLen = t.specialLen > 0 ? 0 : specialLen;
			return;
		}
		
		if(isSpecial){
			if(t.tab.Contains("")){
				specialLen = 0;
			}
			return;
		}
		
		if(t.isSpecial){
			tab.RemoveAll(h => h == "");
			return;
		}
		
		tab.RemoveAll(h => t.tab.Contains(h));
	}
	
	public void RemoveSingle(Table t){
		if(isSpecial && t.isSpecial){
			specialLen = specialLen - t.specialLen;
			return;
		}
		
		if(isSpecial){
			specialLen -= t.tab.Count(h => h == "");
			
			return;
		}
		
		if(t.isSpecial){
			for(int i = 0; i < t.Length; i++){
				tab.Remove("");
			}
			return;
		}
		
		foreach(string h in t.tab){
			tab.Remove(h);
		}
	}
	
	public void RemoveAt(int ind){
		if(isSpecial){
			if(ind >= 0 && ind < specialLen){
				specialLen--;
			}
			
			return;
		}
		
		tab.RemoveAt(ind);
	}
	
	public string[] SplitToChars(){
		return AsString().ToCharArray().Select(c => c.ToString()).ToArray();
	}
	
	public Table Shuffled(){
		Table m = this.Clone();
		
		if(m.isSpecial){
			return m;
		}
		
		for(int i = m.Length - 1; i > 0; i--){
			int j = Table.rand.Next(i + 1); // Random index from 0 to i
			
			// Swap elements
			string temp = m[i];
			m[i] = m[j];
			m[j] = temp;
		}
		
		return m;
	}
	
	public bool Contains(Table t){
		if(t.Length == 0){
			return true;
		}
		
		if(Length == 0){
			return false;
		}
		
		if(t.isSpecial && isSpecial){
			return true;
		}
		
		if(t.isSpecial){
			return tab.Contains("");
		}
		
		if(isSpecial){
			return t.tab.Count == t.tab.Count(h => h == "");
		}
		
		foreach(string h in t.tab){
			if(!tab.Contains(h)){
				return false;
			}
		}
		
		return true;
	}
	
	public bool EqualTo(Table t){
		if(Length != t.Length){
			return false;
		}
		
		if(isSpecial && t.isSpecial){
			return true;
		}
		
		for(int i = 0; i < Length; i++){
			if(this[i] != t[i]){
				return false;
			}
		}
		
		return true;
	}
	
	public static Table GetBool(bool b){
		return b ? True : False;
	}
	
	public string AsString(){
		return isSpecial ? "" : string.Join("", tab);
	}
	
	public override string ToString(){
		return "[" + (isSpecial ? ("#" + Length) : string.Join(", ", tab)) + "]";
	}
}

public record struct TabIndex(TabIndexMode mode, int num){
	public TabIndex(Token k) : this(
		k.type switch{
			TokenType.Number => TabIndexMode.Number,
			TokenType.Random => TabIndexMode.Random,
			TokenType.Length => TabIndexMode.Length,
			_ => TabIndexMode.Number
		},
		k.num
	){
		
	}
	
	public override string ToString(){
		return mode switch{
			TabIndexMode.Number => num.ToString(),
			TabIndexMode.Random => "random",
			TabIndexMode.Length => "length",
			_ => num.ToString()
		};
	}
}

public enum TabIndexMode : byte{
	Number, Random, Length
}