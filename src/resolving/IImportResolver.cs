using TableScript.StandardLibraries;

namespace TableScript;

/// <summary>
/// Interface that resolves import references
/// </summary>
public interface IImportResolver{
	public Action<TableScriptException> OnReport {get; set;}
	
	public ResolvedImport Resolve(string reference, string callingFilename);
}

public class StandardImportResolver : IImportResolver{
	public Action<TableScriptException> OnReport {get; set;}
	
	public virtual ResolvedImport Resolve(string reference, string callingFilename){
		switch(reference){
			case "stdlib":
				return StdLib.TableScriptImport;
			
			case "stdnum":
				return StdNum.TableScriptImport;
			
			case "stdlist":
				return StdList.TableScriptImport;
			
			case "stdregex":
				return StdRegex.TableScriptImport;
			
			default:
				OnReport?.Invoke(new TableScriptException(TableScriptErrorType.Resolver, callingFilename, -1, "Unable to resolve import reference: '" + reference + "'"));
				return new ResolvedImport("standard import resolver error", null, null, null);
		}
	}
}