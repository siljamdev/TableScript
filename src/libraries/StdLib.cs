using System;
using TableScript.Generator;

namespace TableScript.StandardLibraries;

/// <summary>
/// Standard library with useful things. Some things could be replicated with the language, but this implementation in recomended for speed.
/// All functions have the same name in the code and in here
/// </summary>
[TableScriptLibrary("stdlib.cs")]
public static partial class StdLib{
	
	/// <summary>
	/// Maximum table length
	/// </summary>
	[TableScriptGlobal]
	public static readonly int maxLength = Table.MaxLength;
	
	/// <summary>
	/// Operating system, either 'windows', 'linux', 'macos' or ''
	/// </summary>
	[TableScriptGlobal]
	public static string os => OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "macos" : "";
	
	/// <summary>
	/// Print to Standard Output
	/// </summary>
	[TableScriptFunction]
	public static void print(string t){
		Console.WriteLine(t);
	}
	
	/// <summary>
	/// Print to Standard Error
	/// </summary>
	[TableScriptFunction]
	public static void error(string t){
		Console.Error.WriteLine(t);
	}
	
	/// <summary>
	/// Read from Standard Input
	/// </summary>
	[TableScriptFunction]
	public static Table input(string prompt){
		Console.Write(prompt);
		
		if(!Environment.UserInteractive){
			return new Table();
		}
		return new Table(Console.ReadLine());
	}
	
	/// <summary>
	/// Join all elements of a table with a seperator between them
	/// </summary>
	[TableScriptFunction]
	public static string join(Table self, string separator){
		return string.Join(separator, self.contents);
	}
	
	/// <summary>
	/// Split all elements of a table by multiple separators
	/// </summary>
	[TableScriptFunction]
	public static Table split(Table self, Table separators){
		List<string> m = new(self.Length);
		
		foreach(string j in self.contents){
			m.AddRange(j.Split(separators.contents.ToArray(), StringSplitOptions.None));
		}
		
		return new Table(m);
	}
	
	/// <summary>
	/// Split all elements of a table by all common line endings
	/// </summary>
	[TableScriptFunction]
	public static Table splitLines(Table self){
		List<string> m = new(self.Length);
		
		string[] separators = new string[]{"\r\n", "\n", "\r"};
		
		foreach(string j in self.contents){
			m.AddRange(j.Split(separators, StringSplitOptions.None));
		}
		
		return new Table(m);
	}
	
	/// <summary>
	/// True if any element of a table contains a substring
	/// </summary>
	[TableScriptFunction]
	public static bool contains(Table self, string substring){
		return self.contents.Any(s => s.Contains(substring));
	}
	
	/// <summary>
	/// Replace a set of substrings by their replacements
	/// </summary>
	[TableScriptFunction]
	public static Table replace(Table self, Table originals, Table replacements){
		List<string> m = new(self.Length);
		
		int x = Math.Min(originals.Length, replacements.Length);
		
		foreach(string j in self.contents){
			string s = j;
			for(int i = 0; i < x; i++){
				s = s.Replace(originals[i], replacements[i]);
			}
			m.Add(s);
		}
		
		return new Table(m);
	}
	
	/// <summary>
	/// Find the index of an element
	/// </summary>
	[TableScriptFunction]
	public static Table indexOf(Table self, string element){
		return new Table(self.IndexOf(element));
	}
	
	/// <summary>
	/// Transform all elements to uppercase
	/// </summary>
	[TableScriptFunction]
	public static Table upper(Table self){
		return new Table(self.contents.Select(h => h.ToUpper()).ToArray());
	}
	
	/// <summary>
	/// Transform all elements to lowercase
	/// </summary>
	[TableScriptFunction]
	public static Table lower(Table self){
		return new Table(self.contents.Select(h => h.ToLower()).ToArray());
	}
	
	/// <summary>
	/// Trim whitespace from all elements
	/// </summary>
	[TableScriptFunction]
	public static Table trim(Table self){
		return new Table(self.contents.Select(h => h.Trim()).ToArray());
	}
	
	/// <summary>
	/// Trims and removes surrounding double quotes (") from all elements
	/// </summary>
	[TableScriptFunction]
	public static Table removeQuotes(Table self){
		return new Table(self.contents.Select(h => removeQ(h)).ToArray());
	}
	
	/// <summary>
	/// True if a table first elements are the passed ones
	/// </summary>
	[TableScriptFunction]
	public static bool startsWith(Table self, Table elements){
		return self.GetRange(new TabIndex(TabIndexMode.Number, 0), new TabIndex(TabIndexMode.Number, elements.Length)).EqualTo(elements);
	}
	
	/// <summary>
	/// True if a table last elements are the passed ones
	/// </summary>
	[TableScriptFunction]
	public static bool endsWith(Table self, Table elements){
		return self.GetRange(new TabIndex(TabIndexMode.Number, -elements.Length), new TabIndex(TabIndexMode.Number, elements.Length)).EqualTo(elements);
	}
	
	/// <summary>
	/// Delete all matching elements from a table
	/// </summary>
	[TableScriptFunction]
	public static Table deleteAll(Table self, Table toDel){
		Table m = self.Clone();
		m.RemoveAll(toDel);
		return m;
	}
	
	/// <summary>
	/// Delete element at an index
	/// </summary>
	[TableScriptFunction]
	public static Table deleteAt(Table self, Table index){
		Table m = self.Clone();
		m.RemoveAt(index.Length);
		return m;
	}
	
	/// <summary>
	/// Delete all 0-length elements
	/// </summary>
	[TableScriptFunction]
	public static Table deleteEmpty(Table self){
		Table m = self.Clone();
		m.RemoveEmpty();
		return m;
	}
	
	/// <summary>
	/// Reverse the order of a table
	/// </summary>
	[TableScriptFunction]
	public static Table reverse(Table self){
		return self.Reversed();
	}
	
	/// <summary>
	/// Shuffle randomly the order of a table
	/// </summary>
	[TableScriptFunction]
	public static Table shuffle(Table self){
		return self.Shuffled();
	}
	
	/// <summary>
	/// Repeat some elements x times
	/// </summary>
	[TableScriptFunction]
	public static Table repeat(Table self, int times){
		if(self.IsNumber){
			return new Table(self.Length * times);
		}
		
		return new Table(Enumerable.Repeat(self.contents, Math.Max(0, times)).SelectMany(h => h).ToArray());
	}
	
	/// <summary>
	/// Get the maximum table length
	/// </summary>
	[TableScriptFunction][Obsolete("Use stdlib::maxLength global")]
	public static int getMaxLength(){
		return Table.MaxLength;
	}
	
	/// <summary>
	/// Get the operating system, either 'windows', 'linux', 'macos' or ''
	/// </summary>
	[TableScriptFunction][Obsolete("Use stdlib::os global")]
	public static string getOS(){
		return OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "macos" : "";
	}
	
	/// <summary>
	/// Get date and hour in [yy, MM, dd, hh, mm, ss] format
	/// </summary>
	[TableScriptFunction]
	public static Table getDate(){
		DateTime now = DateTime.Now;
		return new Table(now.Year.ToString(), now.Month.ToString(), now.Day.ToString(), now.Hour.ToString(), now.Minute.ToString(), now.Second.ToString());
	}
	
	/// <summary>
	/// Sleep x miliseconds
	/// </summary>
	[TableScriptFunction]
	public static void sleep(int ms){
		Thread.Sleep(ms);
	}
	
	//remove surrounding quotes
	static string removeQ(string p){
		p = p.Trim();
		
		if(p.Length < 1){
			return p;
		}
		if(p[0] == '\"' && p[p.Length - 1] == '\"'){
			if(p.Length < 2){
				return "";
			}
			return p.Substring(1, p.Length - 2);
		}
		return p;
	}
}