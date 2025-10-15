using System;

namespace TabScript;

public static class StdLib{
	public static FunctionStmt[] All => new FunctionStmt[]{print, error, input,
		Join, split, replace,
		upper, lower, trim,
		deleteAll, deleteAt, shuffle, repeat,
		getOS, getDate};
	
	public static readonly FunctionStmt print = new FunctionExtStmt("print", new string[]{"t"}, (Table[] t) => {
		Console.WriteLine(t[0].AsString());
		
		return new Table();
	}, -1);
	
	public static readonly FunctionStmt error = new FunctionExtStmt("error", new string[]{"t"}, (Table[] t) => {
		Console.Error.WriteLine(t[0].AsString());
		
		return new Table();
	}, -1);
	
	public static readonly FunctionStmt input = new FunctionExtStmt("input", new string[]{"prompt"}, (Table[] t) => {
		Console.Write(t[0].AsString());
		
		if(!Environment.UserInteractive){
			return new Table();
		}
		return new Table(Console.ReadLine());
	}, -1);
	
	public static readonly FunctionStmt Join = new FunctionExtStmt("join", new string[]{"self", "separator"}, (Table[] t) => {
		return new Table(string.Join(t[1].AsString(), t[0].contents));
	}, -1);
	
	public static readonly FunctionStmt split = new FunctionExtStmt("split", new string[]{"self", "separator"}, (Table[] t) => {
		List<string> m = new(t[0].Length);
		
		foreach(string j in t[0].contents){
			m.AddRange(j.Split(t[1].contents.ToArray(), StringSplitOptions.None));
		}
		
		return new Table(m);
	}, -1);
	
	public static readonly FunctionStmt replace = new FunctionExtStmt("replace", new string[]{"self", "og", "replacement"}, (Table[] t) => {
		List<string> m = new(t[0].Length);
		
		int x = Math.Min(t[1].Length, t[2].Length);
		
		foreach(string j in t[0].contents){
			string s = j;
			for(int i = 0; i < x; i++){
				s = s.Replace(t[1][i], t[2][i]);
			}
			m.Add(s);
		}
		
		return new Table(m);
	}, -1);
	
	public static readonly FunctionStmt upper = new FunctionExtStmt("upper", new string[]{"self"}, (Table[] t) => {
		return new Table(t[0].contents.Select(h => h.ToUpper()).ToArray());
	}, -1);
	
	public static readonly FunctionStmt lower = new FunctionExtStmt("lower", new string[]{"self"}, (Table[] t) => {
		return new Table(t[0].contents.Select(h => h.ToLower()).ToArray());
	}, -1);
	
	public static readonly FunctionStmt trim = new FunctionExtStmt("trim", new string[]{"self"}, (Table[] t) => {
		return new Table(t[0].contents.Select(h => h.Trim()).ToArray());
	}, -1);
	
	public static readonly FunctionStmt deleteAll = new FunctionExtStmt("deleteAll", new string[]{"self", "toDel"}, (Table[] t) => {
		Table m = t[0];
		m.RemoveAll(t[1]);
		return m;
	}, -1);
	
	public static readonly FunctionStmt deleteAt = new FunctionExtStmt("deleteAt", new string[]{"self", "index"}, (Table[] t) => {
		Table m = t[0];
		m.RemoveAt(t[1].Length);
		return m;
	}, -1);
	
	public static readonly FunctionStmt shuffle = new FunctionExtStmt("shuffle", new string[]{"self"}, (Table[] t) => {
		return t[0].Shuffled();
	}, -1);
	
	public static readonly FunctionStmt repeat = new FunctionExtStmt("repeat", new string[]{"self", "times"}, (Table[] t) => {
		if(t[0].IsNumber){
			return new Table(t[0].Length * t[1].Length);
		}
		
		return new Table(Enumerable.Repeat(t[0].contents, Math.Max(0, t[1].Length)).SelectMany(h => h).ToArray());
	}, -1);
	
	public static readonly FunctionStmt getOS = new FunctionExtStmt("getOS", new string[]{}, (Table[] t) => {
		return new Table(OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "macos" : "");
	}, -1);
	
	public static readonly FunctionStmt getDate = new FunctionExtStmt("getDate", new string[]{}, (Table[] t) => {
		DateTime now = DateTime.Now;
		return new Table(now.Day.ToString(), now.Month.ToString(), now.Year.ToString(), now.Hour.ToString(), now.Minute.ToString(), now.Second.ToString());
	}, -1);
}