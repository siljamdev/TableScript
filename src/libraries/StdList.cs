using System;

namespace TabScript;

/// <summary>
/// Lists are several tables expressed as one, compacted together.
/// All functions have the same name in the code and in here
/// </summary>
public static class StdList{
	
	public static (Delegate func, string description)[] AllFunctions => new (Delegate, string)[]{
		(newLst, "Creates a new list"),
		(isLst, "Test if a table is a valid list"),
		(lstLen, "Get length of the list (number of tables)"),
		(tabLen, "Get length of table at index"),
		(getTab, "Get table at index"),
		(getTabElem, "Get element at index n2 of table at index n1"),
		(addTab, "Add table to the end of the list"),
		(delTab, "Delete table from list at index"),
		(setTab, "Set table at index"),
		(setTabElem, "Set element at index n2 of table at index n1"),
		(insertTab, "Insert table at index"),
		(flattenLst, "Flatten all tables into one table"),
		(containsTab, "Check if list contains table"),
		(indexOfTab, "Get index of table or -1 if not contained"),
		(mergeLst, "Merge 2 lists"),
	};
	
	static ResolvedImport compiled = null;
	
	public static ResolvedImport AsImport {get{
		if(compiled == null){
			compiled = Library.BuildLibrary("stdlist", AllFunctions);
		}
		return compiled;
	}}
	
	const char lenChar = '*';
	
	/// <summary>
	/// Creates a new list
	/// </summary>
	public static Table newLst() => new Table("");
	
	/// <summary>
	/// Test if a table is a valid list
	/// </summary>
	public static Table isLst(Table self){
		if(self.Length < 1){
			return Table.False;
		}
		
		if(self.Length == 1){
			return Table.GetBool(self[0] == "");
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return Table.False;
		}
		
		int contLen = 0;
		
		for(int i = 0; i < tnum; i++){
			if(self[i + 1].Length < 0){
				return Table.False;
			}
			contLen += self[i + 1].Length;
		}
		
		return Table.GetBool(self.Length == 1 + tnum + contLen);
	}
	
	/// <summary>
	/// Get length of the list (number of tables)
	/// </summary>
	public static int lstLen(Table self){
		if(self.Length < 1){
			return -1;
		}
		
		if(self.Length == 1){
			return self[0] == "" ? 0 : -1;
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return -1;
		}
		return tnum;
	}
	
	/// <summary>
	/// Get length of table at index
	/// </summary>
	public static int tabLen(Table self, int index){
		if(self.Length < 2){
			return -1;
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return -1;
		}
		
		if(index < 0 || index >= tnum){
			return -1;
		}
		
		return self[index + 1].Length;
	}
	
	/// <summary>
	/// Get table at index
	/// </summary>
	public static Table getTab(Table self, int index){
		if(self.Length < 2){
			return new Table(0);
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return new Table(0);
		}
		
		if(index < 0 || index >= tnum){
			return new Table(0);
		}
		
		int offset = 1 + tnum;
		for(int i = 0; i < index; i++){
			offset += self[i + 1].Length;
		}
		
		return self.GetRange(new TabIndex(TabIndexMode.Number, offset), new TabIndex(TabIndexMode.Number, self[index + 1].Length));
	}
	
	/// <summary>
	/// Get element at index n2 of table at index n1
	/// </summary>
	public static Table getTabElem(Table self, int tabIndex, int elemIndex){
		if(self.Length < 2){
			return null;
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return null;
		}
		
		if(tabIndex < 0 || tabIndex >= tnum){
			return null;
		}
		
		int tablen = self[1 + tabIndex].Length;
		
		if(elemIndex < 0 || elemIndex >= tablen){
			return null;
		}
		
		int offset = 1 + tnum;
		for(int i = 0; i < tabIndex; i++){
			offset += self[i + 1].Length;
		}
		
		return self.GetElem(new TabIndex(TabIndexMode.Number, offset + elemIndex));
	}
	
	/// <summary>
	/// Add table to the end of the list
	/// </summary>
	public static Table addTab(Table self, Table tab) => Build(Extract(self).Append(tab).ToArray());
	
	/// <summary>
	/// Delete table from list at index
	/// </summary>
	public static Table delTab(Table self, int index){
		if(self.Length < 2){
			return new Table("");
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return new Table("");
		}
		
		if(index < 0 || index >= tnum){
			return self;
		}
		
		Table[] t3 = Extract(self);
		List<Table> t2 = new(tnum - 1);
		for(int i = 0; i < t3.Length; i++){
			if(i == index){
				continue;
			}
			
			t2.Add(t3[i]);
		}
		
		return Build(t2.ToArray());
	}
	
	/// <summary>
	/// Set table at index
	/// </summary>
	public static Table setTab(Table self, int index, Table tab){
		if(self.Length < 2){
			return new Table("");
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return new Table("");
		}
		
		if(index < 0 || index >= tnum){
			return self;
		}
		
		Table[] t3 = Extract(self);
		List<Table> t2 = new(tnum);
		for(int i = 0; i < t3.Length; i++){
			if(i == index){
				t2.Add(tab);
				continue;
			}
			
			t2.Add(t3[i]);
		}
		
		return Build(t2.ToArray());
	}
	
	/// <summary>
	/// Set element at index n2 of table at index n1
	/// </summary>
	public static Table setTabElem(Table self, int tabIndex, int elemIndex, Table element){
		if(self.Length < 2){
			return new Table("");
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return new Table("");
		}
		
		if(tabIndex < 0 || tabIndex >= tnum){
			return self;
		}
		
		int tablen = self[1 + tabIndex].Length;
		
		if(elemIndex < 0 || elemIndex >= tablen){
			return self;
		}
		
		int offset = 1 + tnum;
		for(int i = 0; i < tabIndex; i++){
			offset += self[i + 1].Length;
		}
		
		self.SetElem(new TabIndex(TabIndexMode.Number, offset + elemIndex), element);
		return self;
	}
	
	/// <summary>
	/// Insert table at index
	/// </summary>
	public static Table insertTab(Table self, int index, Table tab){
		if(self.Length < 2){
			return new Table("");
		}
		
		int tnum = self[0].Length;
		
		if(self.Length < tnum + 1){
			return new Table("");
		}
		
		if(index < 0 || index >= tnum){
			return self;
		}
		
		Table[] t3 = Extract(self);
		List<Table> t2 = new(tnum);
		for(int i = 0; i < t3.Length; i++){
			if(i == index){
				t2.Add(tab);
			}
			
			t2.Add(t3[i]);
		}
		
		return Build(t2.ToArray());
	}
	
	/// <summary>
	/// Flatten all tables into one table
	/// </summary>
	public static Table flattenLst(Table self) => new Table(Extract(self).SelectMany(t2 => t2.contents).ToArray());
	
	/// <summary>
	/// Check if list contains table
	/// </summary>
	public static bool containsTab(Table self, Table tab) => Extract(self).Any(t2 => t2.EqualTo(tab));
	
	/// <summary>
	/// Get index of table or -1 if not contained
	/// </summary>
	public static int indexOfTab(Table self, Table tab) => Extract(self).ToList().FindIndex(t2 => t2.EqualTo(tab));
	
	/// <summary>
	/// Merge 2 lists
	/// </summary>
	public static Table mergeLst(Table lst1, Table lst2) => Merge(lst1, lst2);
	
	/// <summary>
	/// List -> Tables
	/// </summary>
	public static Table[] Extract(Table lst){
		if(lst.Length < 2){
			return new Table[0];
		}
		
		int tnum = lst[0].Length;
		
		if(lst.Length < tnum + 1){
			return new Table[0];
		}
		
		Table[] t = new Table[tnum];
		
		int offset = 1 + tnum;
		for(int i = 0; i < tnum; i++){
			int tlen = lst[i + 1].Length;
			
			if(offset + tlen > lst.Length){
				t[i] = new Table();
			}else{
				t[i] = lst.GetRange(new TabIndex(TabIndexMode.Number, offset), new TabIndex(TabIndexMode.Number, tlen));
			}
			
			offset += tlen;
		}
		
		return t;
	}
	
	/// <summary>
	/// Tables -> List
	/// </summary>
	public static Table Build(Table[] t){
		Table lst = new Table();
		lst.Add(new string(lenChar, t.Length));
		foreach(Table tab in t){
			lst.Add(new string(lenChar, tab.Length));
		}
		foreach(Table tab in t){
			lst.AddRange(tab);
		}
		return lst;
	}
	
	/// <summary>
	/// Tables -> List
	/// </summary>
	public static Table Merge(params Table[] lsts){
		return Build(lsts.SelectMany(l => Extract(l)).ToArray());
	}
}