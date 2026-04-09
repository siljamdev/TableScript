using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace TabScript;

/// <summary>
/// The purpose of this class is to transform C# libraries into available functions for TableScript
/// </summary>
public static class Library{
	/// <summary>
	/// Using reflecion, transforms C# functions with Table, string, bool, int and void return types and arguments into FunctionExtStmt records
	/// </summary>
	public static ResolvedImport BuildLibrary(string filename, (Delegate func, string description)[] functions, bool generateDescription = false){
		return new ResolvedImport(filename, null, null, functions.Select(t => BuildSingle(null, t.func, t.description, generateDescription)).Where(f => f != null).ToArray());
	}
	
	/// <summary>
	/// Using reflecion, transforms C# functions with Table, string, bool, int and void return types and arguments into FunctionExtStmt records with the specified names
	/// </summary>
	public static ResolvedImport BuildLibrary(string filename, (string name, Delegate func, string description)[] functions, bool generateDescription = false){
		return new ResolvedImport(filename, null, null, functions.Select(t => BuildSingle(t.name, t.func, t.description, generateDescription)).Where(f => f != null).ToArray());
	}
	
	/// <summary>
	/// Using reflecion, transform a C# function with Table, string, bool, int and void return types and arguments into a FunctionExtStmt record. if 'name' is null, the method's name will be used
	/// </summary>
	public static FunctionExtStmt BuildSingle(string name, Delegate d, string desc, bool generateDescription = false){
		MethodInfo method = d.Method;
		
		ParameterInfo[] parameters = method.GetParameters();
		
		foreach(ParameterInfo p in parameters){
			if(p.IsOut || (p.ParameterType != typeof(Table) && p.ParameterType != typeof(string) && p.ParameterType != typeof(bool) && p.ParameterType != typeof(int))){
				return null;
			}
		}
		
		bool returnsVoid = method.ReturnType == typeof(void);
		bool returnsString = method.ReturnType == typeof(string);
		bool returnsBool = method.ReturnType == typeof(bool);
		bool returnsInt = method.ReturnType == typeof(int);
		bool returnsTable = method.ReturnType == typeof(Table);
		
		if(!returnsVoid && !returnsString && !returnsTable && !returnsBool && !returnsInt){
			return null;
		}
		
		MethodInfo asStringMethod = typeof(Table).GetMethod("AsString", Type.EmptyTypes)!;
		PropertyInfo truthyProperty = typeof(Table).GetProperty("Truthy")!;
		PropertyInfo lengthProperty = typeof(Table).GetProperty("Length")!;
		
		ParameterExpression argsParam = Expression.Parameter(typeof(Table[]), "args");
		
		string[] argsDesc = new string[parameters.Length];
		
		Expression[] callArgs = new Expression[parameters.Length];
		for(int i = 0; i < parameters.Length; i++){
			// args[i]
			
			BinaryExpression argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));
			
			if(parameters[i].ParameterType == typeof(string)){
				callArgs[i] = Expression.Call(argAccess, asStringMethod);
				if(generateDescription){
					argsDesc[i] = "table as string";
				}
			}else if(parameters[i].ParameterType == typeof(bool)){
				callArgs[i] = Expression.Property(argAccess, truthyProperty);
				if(generateDescription){
					argsDesc[i] = "table as boolean";
				}
			}else if(parameters[i].ParameterType == typeof(int)){
				callArgs[i] = Expression.Property(argAccess, lengthProperty);
				if(generateDescription){
					argsDesc[i] = "table as integer";
				}
			}else{
				callArgs[i] = argAccess;
				if(generateDescription){
					argsDesc[i] = "table";
				}
			}
		}
		
		ConstructorInfo stringCtor = typeof(Table).GetConstructor(new[]{typeof(string)})!;
		MethodInfo getBoolMethod = typeof(Table).GetMethod("GetBool", new[]{typeof(bool)})!;
		ConstructorInfo intCtor = typeof(Table).GetConstructor(new[]{typeof(int)})!;
		
		string retDesc = "";
		
		Expression callExpression;
		
		Expression instance = method.IsStatic ? null : Expression.Constant(d.Target);
		
		if(returnsVoid){
			callExpression = Expression.Block(
				Expression.Call(instance, method, callArgs),
				Expression.New(intCtor, Expression.Constant(0))
			);
			if(generateDescription){
				retDesc = "empty table";
			}
		}else if(returnsString){
			callExpression = Expression.New(
				stringCtor,
				Expression.Call(instance, method, callArgs)
			);
			if(generateDescription){
				retDesc = "string as table";
			}
		}else if(returnsBool){
			callExpression = Expression.Call(
				getBoolMethod,
				Expression.Call(instance, method, callArgs)
			);
			if(generateDescription){
				retDesc = "bool as table";
			}
		}else if(returnsInt){
			callExpression = Expression.New(
				intCtor,
				Expression.Call(instance, method, callArgs)
			);
			if(generateDescription){
				retDesc = "integer as table";
			}
		}else{
			callExpression = Expression.Call(instance, method, callArgs);
			if(generateDescription){
				retDesc = "table";
			}
		}
		
		Expression<Func<Table[], Table>> lambda = Expression.Lambda<Func<Table[], Table>>(callExpression, argsParam);
		Func<Table[], Table> compiledWrapper = lambda.Compile();
		
		if(generateDescription){
			desc = "Returns " + retDesc + ", takes as arguments: " + string.Join(", ", argsDesc) + ". " + desc;
		}
		
		return new FunctionExtStmt(name ?? method.Name, parameters.Select((p, i) => p.Name ?? "arg" + i).ToArray(), compiledWrapper, desc);
	}
}