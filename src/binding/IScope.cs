using System;

namespace TableScript;

interface IScope{
	public int define(string filename, int line, string callingImport, string id, bool export);
	
	public int assign(string filename, int line, string callingImport, string id, string im);
	
	public int get(string filename, int line, string callingImport, string id, string im);
	
	public IScope endOfLife();
	
	public Variable getVariable();
}