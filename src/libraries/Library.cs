using System.Linq.Expressions;
using System.Reflection;

namespace TabScript;

/// <summary>
/// The purpose of this class is to transform C# libraries into available functions for TableScript
/// </summary>
public static class Library{
	/// <summary>
	/// Using reflecion, transforms C# functions with Table, string, bool, int and void return types and arguments into FunctionExtStmt records
	/// </summary>
	public static ResolvedImport BuildLibrary(string filename, (Delegate func, string description)[] functions){
		return new ResolvedImport(filename, null, null, functions.Select(t => Wrap(t.func, t.description)).Where(f => f != null).ToArray());
	}
	
	static FunctionExtStmt Wrap(Delegate d, string desc){
		MethodInfo method = d.Method;
		
		if(!method.IsStatic || !method.IsPublic){
			return null;
		}
		
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
		
		Expression[] callArgs = new Expression[parameters.Length];
		for(int i = 0; i < parameters.Length; i++){
			// args[i]
			
			BinaryExpression argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));
			
			if(parameters[i].ParameterType == typeof(string)){
				callArgs[i] = Expression.Call(argAccess, asStringMethod);
			}else if(parameters[i].ParameterType == typeof(bool)){
				callArgs[i] = Expression.Property(argAccess, truthyProperty);
			}else if(parameters[i].ParameterType == typeof(int)){
				callArgs[i] = Expression.Property(argAccess, lengthProperty);
			}else{
				callArgs[i] = argAccess;
			}
		}
		
		ConstructorInfo stringCtor = typeof(Table).GetConstructor(new[]{typeof(string)})!;
		MethodInfo getBoolMethod = typeof(Table).GetMethod("GetBool", new[]{typeof(bool)})!;
		ConstructorInfo intCtor = typeof(Table).GetConstructor(new[]{typeof(int)})!;
		
		Expression callExpression;
		
		if(returnsVoid){
			callExpression = Expression.Block(
				Expression.Call(method, callArgs),
				Expression.Constant(new Table(0))
			);
		}else if(returnsString){
			callExpression = Expression.New(
				stringCtor,
				Expression.Call(method, callArgs)
			);
		}else if(returnsBool){
			callExpression = Expression.Call(
				getBoolMethod,
				Expression.Call(method, callArgs)
			);
		}else if(returnsInt){
			callExpression = Expression.New(
				intCtor,
				Expression.Call(method, callArgs)
			);
		}else{
			callExpression = Expression.Call(method, callArgs);
		}
		
		Expression<Func<Table[], Table>> lambda = Expression.Lambda<Func<Table[], Table>>(callExpression, argsParam);
		Func<Table[], Table> compiledWrapper = lambda.Compile();
		
		return new FunctionExtStmt(method.Name, parameters.Select((p, i) => p.Name ?? "arg" + i).ToArray(), compiledWrapper, desc);
	}
}