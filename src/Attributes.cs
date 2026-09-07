using System;

namespace TableScript.Generator;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TableScriptLibraryAttribute : Attribute{
	public string filename{get;}
	
	public TableScriptLibraryAttribute(string fn){
		filename = fn;
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TableScriptGlobalAttribute : Attribute{}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TableScriptFunctionAttribute : Attribute{}