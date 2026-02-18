namespace TabScript;

public interface IImportResolver{
	public Action<TabScriptException> OnReport {get; set;}
	
	public ResolvedImport Resolve(string import, string callingFilename);
}

public class StandardImportResolver : IImportResolver{	
	public Action<TabScriptException> OnReport {get; set;}
	
	public virtual ResolvedImport Resolve(string import, string callingFilename){
		switch(import){
			case "stdlib":
				return StdLib.AsImport;
			
			case "stdnum":
				return StdNum.AsImport;
			
			case "stdlist":
				return StdList.AsImport;
			
			default:
				OnReport?.Invoke(new TabScriptException(TabScriptErrorType.Resolver, callingFilename, -1, "Unable to resolve import: '" + import + "'"));
				return new ResolvedImport("standard import resolver error", null, null, null);
		}
	}
}