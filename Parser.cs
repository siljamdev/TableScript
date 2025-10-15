using System;

namespace TabScript;

class Parser{
	static void report(TabScriptException x){
		Console.Error.WriteLine(x.ToShortString());
	}
	
	public Action<TabScriptException> OnReport = report;
	
	Token[] tokens;
	int current = 0;
	
	Token curr => tokens[current];
	Token peek => tokens[current];
	Token prev => tokens[current - 1];
	
	bool atEnd => tokens[current].type == TokenType.EOF;
	
	public bool hadError{get; private set;}
	
	public Parser(Token[] t){
		tokens = t;
	}
	
	public Stmt[] Parse(){
		Stmt[] im = imports();
		
		Stmt[] top = topLevel();
		
		Stmt[] funcs = funcDefinitions();
		
		if(hadError){
			throw new TabScriptException(TabScriptErrorType.Parser, -1, "Errors present: Unable to continue");
			return null;
		}else{
			return im.Concat(top).Concat(funcs).ToArray();
		}
	}
	
	Stmt[] imports(){
		List<Stmt> im = new();
		
		while(match(TokenType.Import)){
			im.Add(import());
		}
		
		return im.ToArray();
	}
	
	Stmt import(){
		Token impor = consume(TokenType.Identifier, "Expected identifier after import keyword");
		consume(TokenType.Semicolon, "Expected ';' after import statement");
		
		return new ImportStmt(impor.lex, impor.line); 
	}
	
	Stmt[] topLevel(){
		List<Stmt> s = new();
		
		while(!atEnd && !check(TokenType.Function)){
			s.Add(statement());
		}
		
		return s.ToArray();
	}
	
	Stmt[] funcDefinitions(){
		List<Stmt> defined = new();
		
		while(!atEnd && match(TokenType.Function)){
			try{
				defined.Add(functionDef());
			}catch(TabScriptException pe){
				OnReport(pe);
				sync();
			}
		}
		
		return defined.ToArray();
	}
	
	Stmt functionDef(){
		Token id = consume(TokenType.Identifier, "Expected function identifier after keyword 'function'");
		
		consume(TokenType.LeftPar, "Expected '(' after function identifier");
		
		List<string> parameters = new();
		
		if(!check(TokenType.RightPar)){
			do{
				parameters.Add(consume(TokenType.Identifier, "Expected parameter name").lex);
			}while(match(TokenType.Comma));
		}
		
		consume(TokenType.RightPar, "Expected ')' after parameters");
		
		BlockStmt bod = block();
		
		return new FunctionDefStmt(id.lex, parameters.ToArray(), bod, id.line);
	}
	
	Stmt statement(){
		try{
			if(match(TokenType.Tab)){
				return varDecl();
			}else if(match(TokenType.If)){
				return ifStmt();
			}else if(match(TokenType.While)){
				return whileStmt();
			}else if(match(TokenType.Do)){
				return doStmt();
			}else if(match(TokenType.Foreach)){
				return foreachStmt();
			}else if(match(TokenType.Break)){
				consume(TokenType.Semicolon, "Expected ';' after break statement");
				return new BreakStmt(prev.line);
			}else if(match(TokenType.Continue)){
				consume(TokenType.Semicolon, "Expected ';' after continue statement");
				return new ContinueStmt(prev.line);
			}else if(match(TokenType.Exit)){
				consume(TokenType.Semicolon, "Expected ';' after exit statement");
				return new ExitStmt(prev.line);
			}else if(match(TokenType.Return)){
				return retStmt();
			}else{
				return assignment();
			}
		}catch(TabScriptException pe){
			OnReport(pe);
			sync();
			return null;
		}
	}
	
	Stmt varDecl(){
		Token id = consume(TokenType.Identifier, "Expected variable name after keyword 'tab'");
		
		if(id.lex.Contains(":")){
			error("':' is not allowed in variable identifiers: " + id.lex, id);
		}
		
		Expr val;
		
		if(match(TokenType.Equal)){
			val = expression();
		}else{
			val = new LiteralExpr(new Table());
		}
		
		consume(TokenType.Semicolon, "Expected ';' after variable declaration");
		
		return new VarDeclStmt(id.lex, val, id.line);
	}
	
	Stmt assignment(){
		Expr e = expression();
		
		if(match(TokenType.Equal) || match(TokenType.PlusEqual) || match(TokenType.MinusEqual)){
			Token assig = prev;
			
			if(e is VariableExpr v){
				Expr val = expression();
				consume(TokenType.Semicolon, "Expected ';' after table assignment");
				
				if(assig.type == TokenType.PlusEqual){
					val = new BinaryExpr(e, TokenType.Plus, val);
				}else if(assig.type == TokenType.MinusEqual){
					val = new BinaryExpr(e, TokenType.Minus, val);
				}
				
				return new TabAssignStmt(v.identifier, val, assig.line);
			}else if(e is GetElementExpr g && g.left is VariableExpr vv){
				if(g.ind.mode == TabIndexMode.Length){
					error("Cannot asign to the length of a table", assig);
					return null;
				}
				
				Expr val = expression();
				consume(TokenType.Semicolon, "Expected ';' after element assignment");
				
				if(assig.type == TokenType.MinusEqual){
					error("Cant use '-=' with elements", assig);
					return null;
				}
				
				if(assig.type == TokenType.PlusEqual){
					val = new BinaryExpr(e, TokenType.Plus, val);
				}
				
				return new ElementAssignStmt(vv.identifier, g.ind, val, assig.line);
			}else{
				error("Only variables or variable elements are valid as assignment targets", assig);
				return null;
			}
		}
		
		consume(TokenType.Semicolon, "Expected ';' after expression");
		return new ExprStmt(e, prev.line);
	}
	
	Stmt ifStmt(){
		Token f = prev;
		
		Expr cond = expression();
		
		Stmt then = block();
		
		Stmt els = null;
		
		if(match(TokenType.Else)){
			if(match(TokenType.If)){
				els = ifStmt();
			}else{
				els = block();
			}
		}
		
		return new IfStmt(cond, then, els, f.line);
	}
	
	Stmt whileStmt(){
		Token f = prev;
		
		Expr cond = expression();
		
		Stmt body = block();
		
		Stmt els = null;
		
		if(match(TokenType.Else)){
			els = block();
		}
		
		return new WhileStmt(cond, body, els, f.line);
	}
	
	Stmt doStmt(){
		Token f = prev;
		
		Stmt body = block();
		
		consume(TokenType.While, "Expected 'while' after do body");
		
		Expr cond = expression();
		
		Stmt els = null;
		
		if(match(TokenType.Else)){
			els = block();
		}else{
			consume(TokenType.Semicolon, "Expected ';' or 'else' after do-while condition");
		}
		
		return new DoStmt(cond, body, els, f.line);
	}
	
	Stmt foreachStmt(){
		Token f = prev;
		
		Token id = consume(TokenType.Identifier, "Expected iterated variable identifier");
		
		consume(TokenType.At, "Expected '@' between iterated variable and pool");
		
		Expr pool = expression();
		
		BlockStmt body = block();
		
		Stmt els = null;
		
		if(match(TokenType.Else)){
			els = block();
		}
		
		return new ForeachStmt(id.lex, pool, body, els, f.line);
	}
	
	BlockStmt block(){
		Token st = consume(TokenType.LeftBra, "Expected '{' at block start");
		
		List<Stmt> inn = new();
		
		while(!atEnd && !check(TokenType.RightBra)){
			inn.Add(statement());
		}
		
		consume(TokenType.RightBra, "Expected '}' at block end");
		
		return new BlockStmt(inn.ToArray(), st.line);
	}
	
	Stmt retStmt(){
		Expr val = new LiteralExpr(new Table(0));
		
		if(!check(TokenType.Semicolon)){
			val = expression();
		}
		
		consume(TokenType.Semicolon, "Expected ';' after return statement");
		return new ReturnStmt(val, prev.line);
	}
	
	Expr expression(){
		return ternary();
	}
	
	Expr ternary(){
		Expr exp = or();
		
		if(match(TokenType.QuestionMark)){
			Expr t = expression();
			consume(TokenType.Colon, "Expected ':' in ternary operator");
			Expr f = ternary();
			exp = new TernaryExpr(exp, t, f);
		}
		
		return exp;
	}
	
	Expr or(){
		Expr exp = and();
		
		while(match(TokenType.Or)){
			Expr r = and();
			exp = new BinaryExpr(exp, TokenType.Or, r);
		}
		
		return exp;
	}
	
	Expr and(){
		Expr exp = equality();
		
		while(match(TokenType.And)){
			Expr r = equality();
			exp = new BinaryExpr(exp, TokenType.And, r);
		}
		
		return exp;
	}
	
	Expr equality(){
		Expr exp = comparison();
		
		while(match(TokenType.ExclamationEqual) || match(TokenType.DobEqual)){
			TokenType op = prev.type;
			Expr r = comparison();
			exp = new BinaryExpr(exp, op, r);
		}
		
		return exp;
	}
	
	Expr comparison(){
		Expr exp = membership();
		
		while(match(TokenType.Greater) || match(TokenType.GreaterEqual) || match(TokenType.Less) || match(TokenType.LessEqual)){
			TokenType op = prev.type;
			Expr r = membership();
			exp = new BinaryExpr(exp, op, r);
		}
		
		return exp;
	}
	
	Expr membership(){
		Expr exp = binary();
		
		while(match(TokenType.At)){
			TokenType op = prev.type;
			Expr r = binary();
			exp = new BinaryExpr(exp, op, r);
		}
		
		return exp;
	}
	
	Expr binary(){
		Expr exp = unary();
		
		while(match(TokenType.Plus) || match(TokenType.Minus)){
			TokenType op = prev.type;
			Expr r = unary();
			exp = new BinaryExpr(exp, op, r);
		}
		
		return exp;
	}
	
	Expr unary(){
		if(match(TokenType.Exclamation)){
			TokenType op = prev.type;
			Expr r = unary();
			return new UnaryExpr(op, r);
		}else{
			return postFix();
		}
	}
	
	Expr postFix(){
		Expr exp = primary();
		
		while(true){
			if(match(TokenType.Caret)){
				exp = new UnaryExpr(TokenType.Caret, exp);
			}if(match(TokenType.Percentage)){
				exp = new UnaryExpr(TokenType.Percentage, exp);
			}else if(match(TokenType.Dot)){
				if(match(TokenType.Number) || match(TokenType.Random) || match(TokenType.Length)){
					exp = new GetElementExpr(exp, new TabIndex(prev));
				}else if(match(TokenType.Identifier)){
					string id = prev.lex;
					string imp = null;
					
					if(match(TokenType.DobColon)){
						imp = id;
						id = consume(TokenType.Identifier, "Expected function identifier after '::'").lex;
					}
					
					consume(TokenType.LeftPar, "Expected opening '(' for self-function arguments");
					
					List<Expr> args = new(1);
					
					args.Add(exp);
					
					if(!check(TokenType.RightPar)){
						do{
							args.Add(expression());
						}while(match(TokenType.Comma));
					}
					
					consume(TokenType.RightPar, "Expected closing ')' for self-function arguments");
					
					exp = new CallExpr(id, imp, true, args.ToArray());
				}else{
					error("Expected an index or the keywords 'random' or 'length' after dot access, or a self function", curr);
				}
			}else if(match(TokenType.LeftSq)){
				IndexExpr ind = index(1);
				
				consume(TokenType.Comma, "Expected comma between range arguments");
				
				IndexExpr len = index(2);
				
				exp = new GetRangeExpr(exp, ind, len);
				
				consume(TokenType.RightSq, "Expected closing ']' for range");
			}else{
				break;
			}
		}
		
		while(match(TokenType.Dot) || match(TokenType.DobEqual)){
			TokenType op = prev.type;
			Expr r = comparison();
			exp = new BinaryExpr(exp, op, r);
		}
		
		return exp;
	}
	
	IndexExpr index(int allowed){		
		if(allowed == 1 && match(TokenType.Random)){
			return new IndexExpr(new TabIndex(prev), null);
		}
		
		if(allowed == 2 && match(TokenType.Length)){
			return new IndexExpr(new TabIndex(prev), null);
		}
		
		if(check(TokenType.Number) && curr.num < 0){
			advance();
			return new IndexExpr(new TabIndex(prev), null);
		}
		
		return new IndexExpr(default, expression());
	}
	
	Expr primary(){
		if(match(TokenType.LeftSq)){
			return tabLit();
		}else if(match(TokenType.String)){
			Table t = new Table(prev.obj);
			return new LiteralExpr(t); 
		}else if(match(TokenType.DollarStart)){
			return dollarString();
		}else if(match(TokenType.Number)){			
			return new LiteralExpr(new Table(prev.num));
		}else if(match(TokenType.LeftPar)){
			Expr m = expression();
			
			consume(TokenType.RightPar, "Expected closing ')' for grouping");
			
			return m;
		}else if(match(TokenType.Identifier)){
			if(check(TokenType.LeftPar)){
				return funcCall();
			}else if(check(TokenType.DobColon)){
				return funcCall();
			}else{
				return new VariableExpr(prev.lex);
			}
		}
		
		error("Unexpected Token: " + curr, curr);
		
		return null;
	}
	
	Expr funcCall(){
		string id = prev.lex;
		string imp = null;
		
		if(match(TokenType.DobColon)){
			imp = id;
			id = consume(TokenType.Identifier, "Expected function identifier after '::'").lex;
		}
		
		consume(TokenType.LeftPar, "Expected opening '(' for function arguments");
		
		List<Expr> args = new();
		
		if(!check(TokenType.RightPar)){
			do{
				args.Add(expression());
			}while(match(TokenType.Comma));
		}
		
		consume(TokenType.RightPar, "Expected closing ')' after function arguments");
		
		return new CallExpr(id, imp, false, args.ToArray());
	}
	
	Expr tabLit(){
		List<Expr> parts = new();
		
		if(match(TokenType.RightSq)){
			return new LiteralExpr(new Table());
		}
		
		parts.Add(expression());
		
		while(match(TokenType.Comma)){
			parts.Add(expression());
		}
		
		consume(TokenType.RightSq, "Expected closing ']' for table literal");
		
		return new BuildLiteralExpr(parts.ToArray());
	}
	
	Expr dollarString(){
		List<Expr> parts = new();
		
		if(match(TokenType.DollarEnd)){
			return new LiteralExpr(new Table(""));
		}
		
		while(!match(TokenType.DollarEnd)){
			if(match(TokenType.DollarMidOpen)){
				parts.Add(expression());
				consume(TokenType.DollarMidClose, "Expected closing '}' for dollarstring");
			}else if(match(TokenType.String)){
				parts.Add(new LiteralExpr(new Table(prev.obj)));
			}else{
				error("Unexpected token inside dollarstring: " + curr, curr);
				break;
			}
		}
		
		return new DollarExpr(parts.ToArray());
	}
	
	bool match(TokenType t){
		if(atEnd){
			return false;
		}
		
		if(curr.type == t){
			advance();
			return true;
		}
		
		return false;
	}
	
	bool check(TokenType t){
		if(atEnd){
			return false;
		}
		
		return curr.type == t;
	}
	
	Token advance(){
		if(!atEnd){
			current++;
		}
		
		return prev;
	}
	
	Token consume(TokenType t, string err){
		Token n = advance();
		if(n.type != t){
			error(err, n);
			return null;
		}
		
		return n;
	}
	
	void sync(){
		advance();
		
		while(!atEnd){
			if(prev.type == TokenType.Semicolon){
				return;
			}
			
			switch(peek.type){
				case TokenType.If:
				case TokenType.While:
				case TokenType.Do:
				case TokenType.Foreach:
				case TokenType.Function:
				case TokenType.Tab:
				return;
			}
			
			advance();
		}
	}
	
	void error(string message, Token t){
		hadError = true;
		
		throw new TabScriptException(TabScriptErrorType.Parser, t.line, message);
	}
}