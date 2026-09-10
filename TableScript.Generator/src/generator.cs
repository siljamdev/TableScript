using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Xml.Linq;

namespace TableScript.Generator;

[Generator]
public sealed class Generator : IIncrementalGenerator{
	static INamedTypeSymbol intType;
	static INamedTypeSymbol stringType;
	static INamedTypeSymbol boolType;
	static INamedTypeSymbol tableType;

	public void Initialize(IncrementalGeneratorInitializationContext context){
		//Declare what syntax nodes to watch
		IncrementalValuesProvider<GeneratorInput> classProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "TableScript.Generator.TableScriptLibraryAttribute",
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (ctx, _) => new GeneratorInput((ClassDeclarationSyntax)ctx.TargetNode, (INamedTypeSymbol)ctx.TargetSymbol)
        );

		//Register what to generate when inputs arrive
		var inputProvider = classProvider.Combine(context.CompilationProvider);

		context.RegisterSourceOutput(inputProvider, static (spc, input) => {
			GeneratorInput generatorInput = input.Left;
			Compilation compilation = input.Right;
			
			ClassDeclarationSyntax classNode = generatorInput.classNode;
			INamedTypeSymbol library = generatorInput.symbol;
			
			intType = compilation.GetSpecialType(SpecialType.System_Int32);
			stringType = compilation.GetSpecialType(SpecialType.System_String);
			boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
			tableType = compilation.GetTypeByMetadataName("TableScript.Table");
			
			//Check that the class is partial
            if(!classNode.Modifiers.Any(SyntaxKind.PartialKeyword)){
                spc.ReportDiagnostic(Diagnostic.Create(LibraryMustBePartial, classNode.Identifier.GetLocation(), library.Name));
                return;
            }
			
			AttributeData? attribute = library.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "TableScript.Generator.TableScriptLibraryAttribute");
			if(attribute == null) return;
			string libraryName = (string)attribute!.ConstructorArguments[0].Value!; //One specified on attribute
			
			string className = library.Name;
			string namespaceName = library.ContainingNamespace.ToDisplayString();
			string staticClass = library.IsStatic ? "static" : "";
			
			StringBuilder sb = new();
			
			sb.AppendLine("using TableScript;");
			
			if(!library.ContainingNamespace.IsGlobalNamespace){
				sb.AppendLine($"namespace {library.ContainingNamespace.ToDisplayString()};");
			}
			sb.AppendLine($"{staticClass} partial class {className}{{");
			
			sb.AppendLine($"public {staticClass} string TableScriptFilename = \"{libraryName}\";");
			
			sb.AppendLine($"internal {staticClass } Dictionary<string, Table> _savedGlobals = null;");
			sb.AppendLine($"public {staticClass} Dictionary<string, Table> TableScriptGlobals");
			sb.AppendLine("{get{if(_savedGlobals == null){ _savedGlobals = new Dictionary<string, Table>{");
			
			foreach (ISymbol member in library.GetMembers()){
				if(member is not IFieldSymbol field)
					continue;
				
				AttributeData? attribute2 = field.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "TableScript.Generator.TableScriptGlobalAttribute");
				if(attribute2 == null)
					continue;
				
				if(!field.IsReadOnly){
					spc.ReportDiagnostic(Diagnostic.Create(GlobalMustBeReadonly, field.Locations[0], field.Name));
					continue;
				}
				
				string tab = getAsTable(field.Type, field.Name);
				if(tab == null){
					spc.ReportDiagnostic(Diagnostic.Create(GlobalMustBeType, field.Locations[0], field.Name));
					continue;
				}
				sb.AppendLine("{\"" + field.Name + "\", " + tab + "},");
			}
			
			foreach (ISymbol member in library.GetMembers()){
				if(member is not IPropertySymbol property)
					continue;
				
				AttributeData? attribute2 = property.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "TableScript.Generator.TableScriptGlobalAttribute");
				if(attribute2 == null)
					continue;
				
				if(!property.IsReadOnly){
					spc.ReportDiagnostic(Diagnostic.Create(GlobalMustBeReadonly, property.Locations[0], property.Name));
					continue;
				}
				
				if(property.IsIndexer){
					spc.ReportDiagnostic(Diagnostic.Create(GlobalMustNotBeIndexer, property.Locations[0], property.Name));
					continue;
				}
				
				string tab = getAsTable(property.Type, property.Name);
				if(tab == null){
					spc.ReportDiagnostic(Diagnostic.Create(GlobalMustBeType, property.Locations[0], property.Name));
					continue;
				}
				sb.AppendLine("{\"" + property.Name + "\", " + tab + "},");
			}
			
			sb.AppendLine("};} return _savedGlobals;}}");
			
			sb.AppendLine($"private {staticClass} FunctionStmt[] _savedFuncs = null;");
			sb.AppendLine($"public {staticClass} FunctionStmt[] TableScriptFunctions");
			sb.AppendLine("{get{if(_savedFuncs == null){ _savedFuncs = new FunctionStmt[]{"); //Avoid problems with $
			
			foreach(ISymbol member in library.GetMembers()){
				if(member is not IMethodSymbol method)
					continue;
				
				AttributeData? attribute2 = method.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "TableScript.Generator.TableScriptFunctionAttribute");
				if(attribute2 == null)
					continue;
				
				List<string> args = new();
				
				StringBuilder sbdoc = new();
				
				sbdoc.Append($"Takes {method.Parameters.Length} arguments");
				if(method.Parameters.Length > 0){
					sbdoc.Append(":");
				}
				for(int i = 0; i < method.Parameters.Length; i++){
					IParameterSymbol parameter = method.Parameters[i];
					string tab = getFromTable(parameter.Type, "tables[" + i + "]");
					if(tab == null){
						spc.ReportDiagnostic(Diagnostic.Create(ArgumentMustBeType, parameter.Locations[0], parameter.Name));
						continue;
					}
					args.Add(tab);
					sbdoc.Append(" table " + parameter.Name + " as " + parameter.Type.ToDisplayString());
				}
				sbdoc.Append(". ");
				
				string callExpr = method.Name + "(" + string.Join(", ", args) + ")";
				
				sbdoc.Append("Returns ");
				string ret = null;
				if(method.ReturnsVoid){
					ret = "{" + callExpr + "; return new Table(0);}";
					sbdoc.Append("an empty table. ");
				}else{
					ret = getAsTable(method.ReturnType, callExpr);
					if(ret == null){
						spc.ReportDiagnostic(Diagnostic.Create(FunctionMustBeType, method.Locations[0], method.Name));
						continue;
					}
					sbdoc.Append("table as " + method.ReturnType.ToDisplayString() + ". ");
				}
				
				string? xml = method.GetDocumentationCommentXml();
				if(!string.IsNullOrWhiteSpace(xml)){
					try{
						var doc = XDocument.Parse(xml);
						string? summary = doc.Descendants("summary").FirstOrDefault()?.Value.Trim();
						sbdoc.Append(summary.Replace("\\", "\\\\").Replace("\"", "\\\""));
					}catch{}
				}
				
				int line = method.Locations[0].GetLineSpan().StartLinePosition.Line + 1;
				sb.AppendLine("new FunctionExtStmt(\"" + method.Name + "\", new string[]{" + string.Join(", ", method.Parameters.Select(p => "\"" + p.Name + "\"")) + "}, (tables) => " + ret + ", \"" + sbdoc.ToString() + "\", " + line + "),"); 
			}
			
			sb.AppendLine("};} return _savedFuncs;}}");
			
			sb.AppendLine($"private {staticClass} ResolvedImport _savedImport = null;");
			sb.AppendLine($"public {staticClass} ResolvedImport TableScriptImport");
			sb.AppendLine("{get{if(_savedImport == null){ _savedImport = new ResolvedImport(\"" + libraryName + "\", null, TableScriptGlobals, TableScriptFunctions);} return _savedImport;}}");
			
			sb.AppendLine("}");
			
			//Write the generated source
			spc.AddSource($"{className}.g.cs", sb.ToString());
		});
		
		//Generate attributes
		//context.RegisterPostInitializationOutput(static ctx => {
		//	ctx.AddSource(
		//		"TableScriptAttributes.g.cs",
		//		"""
		//			using System;
		//			
		//			namespace TableScript.Generator;
		//	
		//			[AttributeUsage(AttributeTargets.Class)]
		//			public sealed class TableScriptLibraryAttribute : Attribute{
		//				public string filename{get;}
		//				
		//				public TableScriptLibraryAttribute(string fn){
		//					filename = fn;
		//				}
		//			}
		//			
		//			[AttributeUsage(AttributeTargets.Field)]
		//			public sealed class TableScriptGlobalAttribute : Attribute{}
		//			
		//			[AttributeUsage(AttributeTargets.Method)]
		//			public sealed class TableScriptFunctionAttribute : Attribute{}
		//		"""
		//	);
		//});
	}
	
	private static string getAsTable(ITypeSymbol type, string inner){
		if(SymbolEqualityComparer.Default.Equals(type, intType))
			return "new Table(" + inner + ")";
		else if(SymbolEqualityComparer.Default.Equals(type, stringType))
			return "new Table(" + inner + ")"; 
		else if(SymbolEqualityComparer.Default.Equals(type, boolType))
			return "Table.GetBool(" + inner + ")";
		else if(SymbolEqualityComparer.Default.Equals(type, tableType))
			return "new Table(" + inner + ")";
		else
			return null;
	}
	
	private static string getFromTable(ITypeSymbol type, string table){
		if(SymbolEqualityComparer.Default.Equals(type, intType))
			return table + ".Length";
		else if(SymbolEqualityComparer.Default.Equals(type, stringType))
			return table + ".AsString()"; 
		else if(SymbolEqualityComparer.Default.Equals(type, boolType))
			return table + ".Truthy";
		else if(SymbolEqualityComparer.Default.Equals(type, tableType))
			return "new Table(" + table + ")";
		else
			return null;
	}
	
	private static readonly DiagnosticDescriptor LibraryMustBePartial = new(
		id: "TS001",
		title: "TableScriptLibrary must be partial",
		messageFormat: "Class '{0}' must be declared partial because it is marked with TableScriptLibraryAttribute",
		category: "TableScript",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
    );
	
	private static readonly DiagnosticDescriptor GlobalMustBeReadonly = new(
		id: "TS002",
		title: "TableScriptGlobal must be readonly",
		messageFormat: "Field/Property '{0}' must be declared readonly or not have a set method because it is marked with TableScriptGlobalAttribute",
		category: "TableScript",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
    );
	
	private static readonly DiagnosticDescriptor GlobalMustNotBeIndexer = new(
		id: "TS004",
		title: "TableScriptGlobal must not be an indexer",
		messageFormat: "Property '{0}' must not be an indexer because it is marked with TableScriptGlobalAttribute",
		category: "TableScript",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
    );
	
	private static readonly DiagnosticDescriptor GlobalMustBeType = new(
		id: "TS003",
		title: "TableScriptGlobal invalid type",
		messageFormat: "Field/Property '{0}' must have type Table, string, int or bool because it is marked with TableScriptGlobalAttribute",
		category: "TableScript",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
    );
	
	private static readonly DiagnosticDescriptor ArgumentMustBeType = new(
		id: "TS005",
		title: "TableScriptFunction invalid parameter type",
		messageFormat: "Parameter '{0}' must have type Table, string, int or bool because it is a parameter of a method marked with TableScriptFunctionAttribute",
		category: "TableScript",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
    );
	
	private static readonly DiagnosticDescriptor FunctionMustBeType = new(
		id: "TS006",
		title: "TableScriptFunction invalid return type",
		messageFormat: "Method '{0}' must retyrn type Table, string, int or bool because it is marked with TableScriptFunctionAttribute",
		category: "TableScript",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
    );
	
	private sealed record GeneratorInput(
        ClassDeclarationSyntax classNode,
        INamedTypeSymbol symbol
    );
}