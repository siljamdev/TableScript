using System;
using System.Text.RegularExpressions;
using TableScript.Generator;

namespace TableScript.StandardLibraries;

/// <summary>
/// Standard regex library. Basic regex functionality
/// </summary>
[TableScriptLibrary("stdregex.cs")]
public static partial class StdRegex{
	/// <summary>
	/// True if any element of the table matches the regex
	/// </summary>
	[TableScriptFunction]
	public static bool anyMatch(Table self, string regex){
		return self.contents.Any(a => Regex.IsMatch(a, regex));
	}
	
	/// <summary>
	/// True if all elements of the table match the regex
	/// </summary>
	[TableScriptFunction]
	public static bool allMatch(Table self, string regex){
		return self.contents.All(a => Regex.IsMatch(a, regex));
	}
	
	/// <summary>
	/// Returns the first match found in any of the elements(in order)
	/// </summary>
	[TableScriptFunction]
	public static Table firstMatch(Table self, string regex){
		foreach(string e in self.contents){
			Match m = Regex.Match(e, regex);
			if(!m.Success){
				continue;
			}
			
			return new Table(m.Value);
		}
		
		return Table.False;
	}
	
	/// <summary>
	/// Returns a table with the first match found in any of the elements(in order) followed by its capture groups
	/// </summary>
	[TableScriptFunction]
	public static Table firstMatchGroups(Table self, string regex){
		foreach(string e in self.contents){
			Match m = Regex.Match(e, regex);
			if(!m.Success){
				continue;
			}
			
			Table t = new Table();
			
			foreach(Group g in m.Groups){
				t.Add(g.Value);
			}
			
			return t;
		}
		
		return Table.False;
	}
	
	/// <summary>
	/// Returns a table with all matches of a string (NOT table)
	/// </summary>
	[TableScriptFunction]
	public static Table match(string self, string regex){
		Table t = new();
		
		MatchCollection mc = Regex.Matches(self, regex);
		foreach(Match m in mc){
			t.Add(m.Value);
		}
		
		return t;
	}
	
	/// <summary>
	/// Returns a stdlist list with all matches of a string (NOT table). Each match is a table inside the list, having the match found followed by its capture groups
	/// </summary>
	[TableScriptFunction]
	public static Table matchGroups(string self, string regex){
		List<Table> lst = new();
		
		MatchCollection mc = Regex.Matches(self, regex);
		foreach(Match m in mc){
			Table t = new();
			
			foreach(Group g in m.Groups){
				t.Add(g.Value);
			}
			
			lst.Add(t);
		}
		
		return StdList.Build(lst.ToArray());
	}
	
	/// <summary>
	/// Number of matches in all elements
	/// </summary>
	[TableScriptFunction]
	public static int countMatches(Table self, string regex){
		return self.contents.Sum(e => Regex.Matches(e, regex).Count);
	}
	
	/// <summary>
	/// Replace all matches by their replacement in all elements
	/// </summary>
	[TableScriptFunction]
	public static Table replaceMatches(Table self, string regex, string replacement){
		Table t = new();
		
		foreach(string e in self.contents){
			t.Add(Regex.Replace(e, regex, replacement));
		}
		
		return t;
	}
	
	/// <summary>
	/// Split all elements by a regex separator
	/// </summary>
	[TableScriptFunction]
	public static Table split(Table self, string regex){
		List<string> t = new();
		
		foreach(string e in self.contents){
			t.AddRange(Regex.Split(e, regex));
		}
		
		return new Table(t);
	}
	
	/// <summary>
	/// Find index of first match of a string(NOT table). -1 for no match
	/// </summary>
	[TableScriptFunction]
	public static int indexOfMatch(string self, string regex){
		Match m = Regex.Match(self, regex);
		return m.Success ? m.Index : -1;
	}
	
	/// <summary>
	/// Escapes regex syntax to be a literal
	/// </summary>
	[TableScriptFunction]
	public static string escape(string regex){
		return Regex.Escape(regex);
	}
}