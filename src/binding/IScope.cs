using System;

namespace TabScript;

interface IScope{
	public int define(string filename, int line, string callingImport, string id, bool export, int programCounter);
	
	public int assign(string filename, int line, string callingImport, string id, string im, int programCounter);
	
	public int get(string filename, int line, string callingImport, string id, string im, int programCounter);
	
	public IScope endOfLife();
	
	public Variable getVariable();
}