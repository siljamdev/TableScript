using System;
using System.Text;

namespace TabScript;

class Lexer{
	static readonly Dictionary<string, TokenType> keywords = new(){
		{"if", TokenType.If},
		{"else", TokenType.Else},
		{"function", TokenType.Function},
		{"return", TokenType.Return},
		{"break", TokenType.Break},
		{"continue", TokenType.Continue},
		{"exit", TokenType.Exit},
		{"while", TokenType.While},
		{"do", TokenType.Do},
		{"foreach", TokenType.Foreach},
		{"random", TokenType.Random},
		{"length", TokenType.Length},
		{"tab", TokenType.Tab},
		{"import", TokenType.Import}
	};
	
	static void report(TabScriptException x){
		Console.Error.WriteLine(x.ToShortString());
	}
	
	public Action<TabScriptException> OnReport = report;
	
	string src;
	List<Token> tokens = new();
	
	int line = 1;
	
	int current;
	
	bool atEnd => current >= src.Length;
	
	public bool hadError{get; private set;}
	
	public Lexer(string s){
		src = s;
	}
	
	public Token[] Scan(){
		while(!atEnd){
			ScanNext();
		}
		
		tokens.Add(new Token(TokenType.EOF, null, null, 0, line));
		
		if(hadError){
			throw new TabScriptException(TabScriptErrorType.Lexer, -1, "Errors present: Unable to continue");
			return null;
		}
		
		return tokens.ToArray();
	}
	
	void ScanNext(){
		char c = advance();
		switch(c){
			case '(':
				tokens.Add(create(TokenType.LeftPar));
			break;
			
			case ')':
				tokens.Add(create(TokenType.RightPar));
			break;
			
			case '{':
				tokens.Add(create(TokenType.LeftBra));
			break;
			
			case '}':
				tokens.Add(create(TokenType.RightBra));
			break;
			
			case '[':
				tokens.Add(create(TokenType.LeftSq));
			break;
			
			case ']':
				tokens.Add(create(TokenType.RightSq));
			break;
			
			case ';':
				tokens.Add(create(TokenType.Semicolon));
			break;
			
			case '+':
				tokens.Add(create(match('=') ? TokenType.PlusEqual : TokenType.Plus));
			break;
			
			case '-':
				if(char.IsDigit(peek())){
					number(c);
				}else if(match('=')){
					tokens.Add(create(TokenType.MinusEqual));
				}else{
					tokens.Add(create(TokenType.Minus));
				}
			break;
			
			case '*':
				tokens.Add(create(TokenType.Star));
			break;
			
			case '^':
				tokens.Add(create(TokenType.Caret));
			break;
			
			case '%':
				tokens.Add(create(TokenType.Percentage));
			break;
			
			case '@':
				tokens.Add(create(TokenType.At));
			break;
			
			case '.':
				tokens.Add(create(TokenType.Dot));
			break;
			
			case ',':
				tokens.Add(create(TokenType.Comma));
			break;
			
			case '&':
				tokens.Add(create(TokenType.And));
			break;
			
			case '|':
				tokens.Add(create(TokenType.Or));
			break;
			
			case '?':
				tokens.Add(create(TokenType.QuestionMark));
			break;
			
			case ':':
				tokens.Add(create(match(':') ? TokenType.DobColon : TokenType.Colon));
			break;
			
			case '$':
				if(!match('"')){
					error("Expected '\"' to start dollarstring");
				}else{
					dollarStr();
				}
			break;
			
			case '!':
				tokens.Add(create(match('=') ? TokenType.ExclamationEqual : TokenType.Exclamation));
			break;
			
			case '=':
				tokens.Add(create(match('=') ? TokenType.DobEqual : TokenType.Equal));
			break;
			
			case '>':
				tokens.Add(create(match('=') ? TokenType.GreaterEqual : TokenType.Greater));
			break;
			
			case '<':
				tokens.Add(create(match('=') ? TokenType.LessEqual : TokenType.Less));
			break;
			
			case '/':
				if(!match('/')){
					error("Expected another / to start comment");
				}else{
					while(peek() != '\n' && !atEnd){
						advance();
					}
				}
			break;
			
			case '"':
				str();
			break;
			
			case ' ':
			case '\t':
			case '\r':
			break;
			
			case '\n':
				line++;
			break;
			
			default:
				if(char.IsLetter(c)){
					identifier(c);
				}else if(char.IsDigit(c)){
					number(c);
				}else{
					error("Unexpected char: " + c);
				}
			break;
		}
	}
	
	void number(char c){
		StringBuilder sb = new();
		sb.Append(c);
		
		while(char.IsDigit(peek())){
			sb.Append(advance());
		}
		
		string f = sb.ToString();
		
		if(int.TryParse(f, out int i)){
			tokens.Add(new Token(TokenType.Number, null, null, i, line));
		}else{
			error("Invalid number: " + f);
		}
	}
	
	void identifier(char c){
		StringBuilder sb = new();
		sb.Append(c);
		while(char.IsLetterOrDigit(peek()) || peek() == '_'){
			sb.Append(advance());
		}
		
		string f = sb.ToString();
		
		if(keywords.ContainsKey(f)){
			tokens.Add(create(keywords[f]));
		}else{
			tokens.Add(new Token(TokenType.Identifier, f, null, 0, line));
		}
	}
	
	void dollarStr(){
		tokens.Add(create(TokenType.DollarStart));
		
		bool escapeSeqPrev = false;
		
		StringBuilder sb = new();
		
		while(!atEnd){
			if(peek() == '\n'){
				error("Unterminated dollarstring at line end");
				break;
			}
			
			if(!escapeSeqPrev){
				if(peek() == '\\'){
					escapeSeqPrev = true;
					advance();
				}else if(peek() == '"'){
					advance();
					
					if(sb.Length > 0){
						tokens.Add(new Token(TokenType.String, null, sb.ToString(), 0, line));
					}
					tokens.Add(create(TokenType.DollarEnd));
					return;
				}else if(peek() == '{'){
					if(sb.Length > 0){
						tokens.Add(new Token(TokenType.String, null, sb.ToString(), 0, line));
					}
					sb.Clear();
					
					tokens.Add(create(TokenType.DollarMidOpen));
					advance();
					
					while(peek() != '}'){
						if(atEnd){
							error("Unterminated dollarstring at line end");
							break;
						}
						
						ScanNext();
					}
					
					if(peek() == '}'){
						advance();
						tokens.Add(create(TokenType.DollarMidClose));
					}else{
						error("Unterminated dollarstring insertion: missing '}'");
					}
				}else{
					sb.Append(advance());
				}
			}else{
				if(peek() == 'n'){
					sb.Append('\n');
				}else if(peek() == '\\'){
					sb.Append('\\');
				}else if(peek() == '"'){
					sb.Append('"');
				}else if(peek() == '{'){
					sb.Append('{');
				}else{
					error("Unknown escape sequence in dollarstring: \\" + peek());
				}
				
				escapeSeqPrev = false;
				advance();
			}
		}
		
		if(atEnd){
			error("Unterminated dollarstring at file end");
		}
		
		if(sb.Length > 0){
			tokens.Add(new Token(TokenType.String, null, sb.ToString(), 0, line));
		}
		tokens.Add(create(TokenType.DollarEnd));
	}
	
	void str(){
		bool escapeSeqPrev = false;
		
		StringBuilder sb = new();
		
		while(!atEnd){
			if(peek() == '\n'){
				error("Unterminated string at line end");
				break;
			}
			
			if(!escapeSeqPrev){
				if(peek() == '\\'){
					escapeSeqPrev = true;
					advance();
				}else if(peek() == '"'){
					advance();
					tokens.Add(new Token(TokenType.String, null, sb.ToString(), 0, line));
					return;
				}else{
					sb.Append(advance());
				}
			}else{
				if(peek() == 'n'){
					sb.Append('\n');
				}else if(peek() == '\\'){
					sb.Append('\\');
				}else if(peek() == '"'){
					sb.Append('"');
				}else{
					error("Unknown escape sequence in string: \\" + peek());
				}
				
				escapeSeqPrev = false;
				advance();
			}
		}
		
		if(atEnd){
			error("Unterminated string at file end");
		}
		
		tokens.Add(new Token(TokenType.String, null, sb.ToString(), 0, line));
	}
	
	Token create(TokenType t){
		return new Token(t, null, null, 0, line);
	}
	
	char advance(){
		current++;
		return src[current - 1];
	}
	
	bool match(char c){
		if(atEnd){
			return false;
		}
		
		if(src[current] != c){
			return false;
		}
		
		current++;
		return true;
	}
	
	char peek(){
		if(atEnd){
			return '\0';
		}
		return src[current];
	}
	
	void error(string message){
		OnReport(new TabScriptException(TabScriptErrorType.Lexer, line, message));
		hadError = true;
	}
}