using System;

namespace TabScript;

/// <summary>
/// Standard library with useful things. Some things could be replicated with the language, but this implementation in recomended for speed.
/// All functions have the same name in the code and in here
/// </summary>
public static class StdLib{
	
	public static (Delegate func, string description)[] AllFunctions => new (Delegate, string)[]{
		(print, "Print to Standard Output"),
		(error, "Print to Standard Error"),
		(input, "Read from Standard Input"),
		(join, "Join all elements of a table with a seperator between them"),
		(split, "Split all elements of a table by multiple separators"),
		(replace, "Replace a set of substrings by their replacement"),
		(indexOf, "Find the index of an element"),
		(upper, "Transform all elements to uppercase"),
		(lower, "Transform all elements to lowercase"),
		(trim, "Trim whitespace from all elements"),
		(deleteAll, "Delete all matching elements from a table"),
		(deleteAt, "Delete element at an index"),
		(reverse, "Reverse the order of a table"),
		(shuffle, "Shuffle randomly the order of a table"),
		(repeat, "Repeat some elements x times"),
		(getOS, "Get the operating system"),
		(getDate, "Get date and hour in [yy, MM, dd, hh, mm, ss] format"),
		(sleep, "Sleep x miliseconds"),
	};
	
	static ResolvedImport compiled = null;
	
	public static ResolvedImport AsImport {get{
		if(compiled == null){
			compiled = Library.BuildLibrary("stdlib", AllFunctions);
		}
		return compiled;
	}}
	
	/// <summary>
	/// Print to Standard Output
	/// </summary>
	public static void print(string t){
		Console.WriteLine(t);
	}
	
	/// <summary>
	/// Print to Standard Error
	/// </summary>
	public static void error(string t){
		Console.Error.WriteLine(t);
	}
	
	/// <summary>
	/// Read from Standard Input
	/// </summary>
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
	public static string join(Table self, string separator){
		return string.Join(separator, self.contents);
	}
	
	/// <summary>
	/// Split all elements of a table by multiple separators
	/// </summary>
	public static Table split(Table self, Table separators){
		List<string> m = new(self.Length);
		
		foreach(string j in self.contents){
			m.AddRange(j.Split(separators.contents.ToArray(), StringSplitOptions.None));
		}
		
		return new Table(m);
	}
	
	/// <summary>
	/// Replace a set of substrings by their replacement
	/// </summary>
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
	public static Table indexOf(Table self, string element){
		return new Table(self.IndexOf(element));
	}
	
	/// <summary>
	/// Transform all elements to uppercase
	/// </summary>
	public static Table upper(Table self){
		return new Table(self.contents.Select(h => h.ToUpper()).ToArray());
	}
	
	/// <summary>
	/// Transform all elements to lowercase
	/// </summary>
	public static Table lower(Table self){
		return new Table(self.contents.Select(h => h.ToUpper()).ToArray());
	}
	
	/// <summary>
	/// Trim whitespace from all elements
	/// </summary>
	public static Table trim(Table self){
		return new Table(self.contents.Select(h => h.Trim()).ToArray());
	}
	
	/// <summary>
	/// Delete all matching elements from a table
	/// </summary>
	public static Table deleteAll(Table self, Table toDel){
		Table m = self.Clone();
		m.RemoveAll(toDel);
		return m;
	}
	
	/// <summary>
	/// Delete element at an index
	/// </summary>
	public static Table deleteAt(Table self, Table index){
		Table m = self.Clone();
		m.RemoveAt(index.Length);
		return m;
	}
	
	/// <summary>
	/// Reverse the order of a table
	/// </summary>
	public static Table reverse(Table self){
		return self.Reversed();
	}
	
	/// <summary>
	/// Shuffle randomly the order of a table
	/// </summary>
	public static Table shuffle(Table self){
		return self.Shuffled();
	}
	
	/// <summary>
	/// Repeat some elements x times
	/// </summary>
	public static Table repeat(Table self, int times){
		if(self.IsNumber){
			return new Table(self.Length * times);
		}
		
		return new Table(Enumerable.Repeat(self.contents, Math.Max(0, times)).SelectMany(h => h).ToArray());
	}
	
	/// <summary>
	/// Get the operating system
	/// </summary>
	public static string getOS(){
		return OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "macos" : "";
	}
	
	/// <summary>
	/// Get date and hour in [yy, MM, dd, hh, mm, ss] format
	/// </summary>
	public static Table getDate(){
		DateTime now = DateTime.Now;
		return new Table(now.Year.ToString(), now.Month.ToString(), now.Day.ToString(), now.Hour.ToString(), now.Minute.ToString(), now.Second.ToString());
	}
	
	/// <summary>
	/// Sleep x miliseconds
	/// </summary>
	public static void sleep(int ms){
		Thread.Sleep(ms);
	}
}