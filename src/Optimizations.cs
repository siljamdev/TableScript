using System;

namespace TableScript;

/// <summary>
/// Enum to enable/disable optimizations
/// </summary>
[Flags]
public enum Optimizations{
	None = 0,
	
	/// <summary>
	/// Expressions with literal values will be computed on compile-time
	/// </summary>
	ConstantFolding = 1, //Optimizer
	
	/// <summary>
	/// Literal valued conditions in loops will drop a branch completely
	/// </summary>
	ConstantBranching = 2, //Optimizer
	
	/// <summary>
	/// Expressions will be simplified
	/// </summary>
	ExpressionSimplification = 4, //Optimizer
	
	/// <summary>
	/// Eliminate unused functions
	/// </summary>
	DeadFunctionElimination = 8, //Binder
	
	/// <summary>
	/// Eliminate unreachable code
	/// </summary>
	DeadCodeElimination = 16, //Optimizer
	
	/// <summary>
	/// Optimize before resolving
	/// </summary>
	EarlyOptimizations = 32, //Early optimizer
	
	/// <summary>
	/// WARNING: DESTRUCTIVE! Eliminates unreachable code in the early optimization pass before or checking it for errors
	/// </summary>
	EarlyDeadCodeElimination = 64, //Optimizer
	
	/// <summary>
	/// Reuse stack indexes for variables
	/// </summary>
	VariableIndexReusing = 128, //Allocator
	
	/// <summary>
	/// Substitute known values of variables
	/// </summary>
	ConstantPropagation = 256, //Optimizer + Allocator
	
	/// <summary>
	/// Eliminate unneeded variable stores
	/// </summary>
	DeadStoreElimination = 512, //Optimizer + Allocator
	
	
	
	/// <summary>
	/// Default profile
	/// </summary>
	Normal = ConstantFolding | ConstantBranching | ExpressionSimplification | DeadFunctionElimination | DeadCodeElimination,
	
	/// <summary>
	/// Optimize early, useful for shortening source length of imports
	/// </summary>
	Early = EarlyOptimizations | ConstantFolding | ConstantBranching | ExpressionSimplification,
	
	/// <summary>
	/// Optimize early at the expense of ignoring potentially erroneous code, useful for shortening source length of imports
	/// </summary>
	EarlyDestructive = EarlyOptimizations | ConstantFolding | ConstantBranching | ExpressionSimplification | EarlyDeadCodeElimination,
	
	/// <summary>
	/// Useful for when unsued functions will be called from the API
	/// </summary>
	ExternalCall = Normal & ~DeadFunctionElimination,
	
	/// <summary>
	/// Experimental optimizations
	/// </summary>
	Experimental = Normal | VariableIndexReusing | ConstantPropagation | DeadStoreElimination
}