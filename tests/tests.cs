using System;
using TabScript;

class Tests{
	static int passed = 0;
	static int failed = 0;
	
	static int testNum = 0;
	
	public static int Main(){
		bool fileError = false;
		foreach(string file in Directory.GetFiles("tests", "*.tbs")){
			TableScript f;
			try{
				f = TableScript.FromSource(file, File.ReadAllText(file), false);
				f.Run(); //Needed to use CallFunction
			}catch(Exception e){
				Console.Error.WriteLine("\nFailed to compile '" + file + "'");
				Console.Error.WriteLine(e);
				
				fileError = true;
				
				continue;
			}
			
			testScript(f);
		}
		
		Console.WriteLine("\n" + (passed + failed) + " tests run");
		Console.WriteLine(" " + passed + " tests passed");
		Console.WriteLine(" " + failed + " tests failed");
		
		if(fileError){
			return 1;
		}else if(failed > 0){
			return 2;
		}else{
			return 0;
		}
	}
	
	static void testScript(TableScript s){
		Console.WriteLine("\nRunning tests of '" + s.body.filename + "'");
		
		foreach(TabFunc f in s.functions){
			if(f.identifier.StartsWith("test_")){
				testNum++;
				
				Table r = s.CallFunction(f.import, f.identifier);
				if(r.Truthy){
					Console.WriteLine("    Test " + testNum + " passed: " + f.identifier);
					passed++;
				}else{
					Console.Error.WriteLine("[X] Test " + testNum + " failed: " + f.identifier);
					failed++;
				}
			}
		}
	}
}