using TableScript.StandardLibraries;

namespace TableScript;

/// <summary>
/// Interface that resolves import references
/// </summary>
public interface IImportResolver{
	public Action<TabScriptException> OnReport {get; set;}
	
	public ResolvedImport Resolve(string reference, string callingFilename);
}

public class StandardImportResolver : IImportResolver{
	public Action<TabScriptException> OnReport {get; set;}
	
	public virtual ResolvedImport Resolve(string reference, string callingFilename){
		switch(reference){
			case "stdlib":
				return StdLib.AsImport;
			
			case "stdnum":
				return StdNum.AsImport;
			
			case "stdlist":
				return StdList.AsImport;
			
			case "stdregex":
				return StdRegex.AsImport;
			
			default:
				OnReport?.Invoke(new TabScriptException(TabScriptErrorType.Resolver, callingFilename, -1, "Unable to resolve import reference: '" + reference + "'"));
				return new ResolvedImport("standard import resolver error", null, null, null);
		}
	}
}