using System;

namespace TabScript;

[Flags]
public enum Optimizations{
	None = 0,
	ConstantFolding = 1, //Optimizer
	ConstantBranching = 2, //Optimizer
	ExpressionSimplification = 4, //Optimizer
	DeadFunctionElimination = 8, //Binder
	DeadCodeElimination = 16, //Optimizer. NEVER on early, its disabled
	OptimizeBeforeResolving = 32, //Early optimizer
	VariableIndexReusing = 64, //Indexer
	
	Normal = ConstantFolding | ConstantBranching | ExpressionSimplification | DeadFunctionElimination | DeadCodeElimination | VariableIndexReusing,
	Early = ConstantFolding | ConstantBranching | ExpressionSimplification | OptimizeBeforeResolving,
	ExternalCall = ConstantFolding | ConstantBranching | ExpressionSimplification | DeadCodeElimination | VariableIndexReusing
}