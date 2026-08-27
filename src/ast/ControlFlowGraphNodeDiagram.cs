using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableScript;

partial class CFGNode{
	private class Block{
		public List<string> Lines = new();
		public int Width;
		public int Center;
	}
	
	public static string ToDiagram(CFGNode entry, int maxWidth = -1){
		if(entry == null){
			return "(empty graph)";
		}
		if(maxWidth < 1){
			maxWidth = Console.WindowHeight - 1;
		}
		
		maxWidth = Math.Max(12, maxWidth);
		
		HashSet<CFGNode> seen = new();
		Block block = RenderNode(entry, maxWidth, seen);
		return string.Join("\n", block.Lines);
	}
	
	private static Block RenderNode(CFGNode n, int allowedWidth, HashSet<CFGNode> seen){
		if(n == null){
			return null;
		}
		
		int contentWidth = Math.Max(6, allowedWidth - 4);
		bool isCond = n is CondCFGNode;
		
		if(!seen.Add(n)){
			List<string> stub = new(){ "#" + n.id };
			stub.AddRange(Wrap("ENTRIES: " + EntriesText(n), contentWidth));
			stub.Add("(see above)");
			return MakeBox(stub, isCond);
		}
		
		Block box = MakeBox(BuildContent(n, contentWidth), isCond);
		
		switch(n){
			case StmtCFGNode s:
				if(s.next == null){
					return box;
				}
				Block next = RenderNode(s.next, allowedWidth, seen);
				return StackVertical(box, next, null);
			
			case CondCFGNode c:{
				Block tB = c.isTrue != null ? RenderNode(c.isTrue, allowedWidth, seen) : null;
				Block fB = c.isFalse != null ? RenderNode(c.isFalse, allowedWidth, seen) : null;
				
				if(tB == null && fB == null){
					return box;
				}
				if(tB != null && fB != null){
					if(tB.Width + 3 + fB.Width <= allowedWidth){
						return BranchBoth(box, tB, fB);
					}
					Block withT = StackVertical(box, tB, "T");
					return StackVertical(withT, fB, "F");
				}
				if(tB != null){
					return StackVertical(box, tB, "T");
				}
				return StackVertical(box, fB, "F");
			}
			
			default:
				return box;
		}
	}
	
	private static string EntriesText(CFGNode n){
		return n.entries.Count == 0 ? "(none)" : string.Join(" ", n.entries.Select(e => "\u2191#" + e.id));
	}
	
	private static List<string> BuildContent(CFGNode n, int contentWidth){
		List<string> content = new(){ "#" + n.id };
		content.AddRange(Wrap("ENTRIES: " + EntriesText(n), contentWidth));
		
		switch(n){
			case StmtCFGNode s:
				if(s.statements == null || s.statements.Length == 0){
					content.Add("(empty)");
				}else{
					foreach(Stmt stmt in s.statements){
						content.AddRange(Wrap(stmt?.ToString() ?? "null", contentWidth));
					}
				}
				break;
			
			case CondCFGNode c:
				content.AddRange(Wrap("IF " + (c.condition?.ToString() ?? "?"), contentWidth));
				break;
			
			case DummyCFGNode:
				content.Add("(dummy)");
				break;
			
			case RedirectCFGNode:
				content.Add("(redirect)");
				break;
		}
		
		return content;
	}
	
	private static Block MakeBox(List<string> content, bool doubleLine){
		int innerWidth = content.Count == 0 ? 0 : content.Max(l => l.Length);
		char h = doubleLine ? '\u2550' : '\u2500';
		char v = doubleLine ? '\u2551' : '\u2502';
		char tl = doubleLine ? '\u2554' : '\u250C';
		char tr = doubleLine ? '\u2557' : '\u2510';
		char bl = doubleLine ? '\u255A' : '\u2514';
		char br = doubleLine ? '\u255D' : '\u2518';
		
		List<string> lines = new(){ tl + new string(h, innerWidth + 2) + tr };
		foreach(string line in content){
			lines.Add(v + " " + line.PadRight(innerWidth) + " " + v);
		}
		lines.Add(bl + new string(h, innerWidth + 2) + br);
		
		int width = innerWidth + 4;
		return new Block{ Lines = lines, Width = width, Center = width / 2 };
	}
	
	private static Block CenterPad(Block b, int newWidth){
		if(newWidth <= b.Width){
			return b;
		}
		int leftPad = (newWidth - b.Width) / 2;
		int rightPad = newWidth - b.Width - leftPad;
		List<string> lines = b.Lines.Select(l => new string(' ', leftPad) + l + new string(' ', rightPad)).ToList();
		return new Block{ Lines = lines, Width = newWidth, Center = b.Center + leftPad };
	}
	
	private static Block StackVertical(Block top, Block bottom, string label){
		int width = Math.Max(top.Width, bottom.Width);
		Block t = CenterPad(top, width);
		Block b = CenterPad(bottom, width);
		int col = Clamp(t.Center, width);
		
		char[] row1 = new string(' ', width).ToCharArray();
		row1[col] = '\u2502';
		if(label != null){
			PlaceLabel(row1, col + 2, label, width);
		}
		
		char[] row2 = new string(' ', width).ToCharArray();
		row2[col] = '\u25BC';
		
		List<string> lines = new();
		lines.AddRange(t.Lines);
		lines.Add(new string(row1));
		lines.Add(new string(row2));
		lines.AddRange(b.Lines);
		
		return new Block{ Lines = lines, Width = width, Center = col };
	}
	
	private static Block BranchBoth(Block box, Block left, Block right){
		int gap = 3;
		int childrenWidth = left.Width + gap + right.Width;
		int leftCenterInChildren = left.Center;
		int rightCenterInChildren = left.Width + gap + right.Center;
		
		List<string> childLines = new();
		int rows = Math.Max(left.Lines.Count, right.Lines.Count);
		for(int i = 0; i < rows; i++){
			string l = i < left.Lines.Count ? left.Lines[i] : new string(' ', left.Width);
			string r = i < right.Lines.Count ? right.Lines[i] : new string(' ', right.Width);
			childLines.Add(l + new string(' ', gap) + r);
		}
		
		int totalWidth = Math.Max(box.Width, childrenWidth);
		Block paddedBox = CenterPad(box, totalWidth);
		int padLeft = Math.Max(0, (totalWidth - childrenWidth) / 2);
		int padRight = Math.Max(0, totalWidth - childrenWidth - padLeft);
		List<string> paddedChildLines = childLines.Select(l => new string(' ', padLeft) + l + new string(' ', padRight)).ToList();
		
		int leftCol = Clamp(padLeft + leftCenterInChildren, totalWidth);
		int rightCol = Clamp(padLeft + rightCenterInChildren, totalWidth);
		int parentCol = Clamp(paddedBox.Center, totalWidth);
		
		char[] row1 = new string(' ', totalWidth).ToCharArray();
		row1[parentCol] = '\u2502';
		
		char[] row2 = new string(' ', totalWidth).ToCharArray();
		int lo = Math.Min(leftCol, rightCol);
		int hi = Math.Max(leftCol, rightCol);
		for(int x = lo; x <= hi; x++){
			row2[x] = '\u2500';
		}
		row2[leftCol] = '\u250C';
		row2[rightCol] = '\u2510';
		row2[parentCol] = '\u2534';
		
		char[] row3 = new string(' ', totalWidth).ToCharArray();
		row3[leftCol] = '\u2502';
		row3[rightCol] = '\u2502';
		PlaceLabel(row3, leftCol + 2, "T", totalWidth);
		PlaceLabel(row3, rightCol + 2, "F", totalWidth);
		
		char[] row4 = new string(' ', totalWidth).ToCharArray();
		row4[leftCol] = '\u25BC';
		row4[rightCol] = '\u25BC';
		
		List<string> lines = new();
		lines.AddRange(paddedBox.Lines);
		lines.Add(new string(row1));
		lines.Add(new string(row2));
		lines.Add(new string(row3));
		lines.Add(new string(row4));
		lines.AddRange(paddedChildLines);
		
		return new Block{ Lines = lines, Width = totalWidth, Center = parentCol };
	}
	
	private static int Clamp(int x, int width){
		return Math.Max(0, Math.Min(width - 1, x));
	}
	
	private static void PlaceLabel(char[] row, int pos, string label, int width){
		for(int i = 0; i < label.Length; i++){
			int p = pos + i;
			if(p >= 0 && p < width){
				row[p] = label[i];
			}
		}
	}
	
	private static List<string> Wrap(string text, int width){
		if(width < 1){
			width = 1;
		}
		List<string> result = new();
		if(string.IsNullOrEmpty(text)){
			result.Add("");
			return result;
		}
		
		foreach(string rawLine in text.Split('\n')){
			string[] words = rawLine.Split(' ');
			StringBuilder cur = new();
			foreach(string w in words){
				string word = w;
				while(word.Length > width){
					if(cur.Length > 0){
						result.Add(cur.ToString());
						cur.Clear();
					}
					result.Add(word.Substring(0, width));
					word = word.Substring(width);
				}
				if(cur.Length == 0){
					cur.Append(word);
				}else if(cur.Length + 1 + word.Length <= width){
					cur.Append(' ').Append(word);
				}else{
					result.Add(cur.ToString());
					cur.Clear();
					cur.Append(word);
				}
			}
			result.Add(cur.ToString());
		}
		
		return result;
	}
}