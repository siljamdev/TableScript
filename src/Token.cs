using System;

namespace TabScript;

record Token(TokenType type, string lex, string obj, int num, int line){
	public override string ToString(){
		return type switch {
			TokenType.Identifier => lex,
			TokenType.String => obj,
			TokenType.Number => num.ToString(),
			_ => GetAsString(type)
        };
	}
	
	public static string GetAsString(TokenType type){
		return type switch {
			TokenType.LeftPar  => "(",
			TokenType.RightPar => ")",
			TokenType.LeftBra  => "{",
			TokenType.RightBra => "}",
			TokenType.LeftSq   => "[",
			TokenType.RightSq  => "]",
			TokenType.Semicolon => ";",
			TokenType.Plus     => "+",
			TokenType.Minus     => "-",
			TokenType.Star     => "*",
			TokenType.Caret     => "^",
			TokenType.Percentage     => "%",
			TokenType.At     => "@",
			TokenType.Dot      => ".",
			TokenType.Comma    => ",",
			TokenType.And      => "&",
			TokenType.Or       => "|",
			TokenType.QuestionMark       => "?",
			TokenType.Colon       => ":",
			TokenType.DollarStart       => "$\"",
			TokenType.DollarMidOpen       => "{",
			TokenType.DollarMidClose       => "}",
			TokenType.DollarEnd       => "\"",
			TokenType.Exclamation => "!",
			TokenType.ExclamationEqual => "!=",
			TokenType.Equal    => "=",
			TokenType.PlusEqual    => "+=",
			TokenType.MinusEqual    => "-=",
			TokenType.DobEqual => "==",
			TokenType.Greater  => ">",
			TokenType.GreaterEqual => ">=",
			TokenType.Less     => "<",
			TokenType.LessEqual => "<=",
			TokenType.DobColon => "::",
			_ => type.ToString() // for Identifier, String, Number, keywords, EOF, etc.
		};
	}
}

record TokenList(string filename, Token[] tokens);

enum TokenType{
	LeftPar, RightPar,
	LeftBra, RightBra,
	LeftSq, RightSq,
	Semicolon,
	Plus, Minus,
	Star,
	Caret, Percentage,
	At,
	Dot, Comma,
	And, Or,
	
	QuestionMark,
	Colon,
	
	DollarStart,
	DollarMidOpen,
	DollarMidClose,
	DollarEnd,
	
	Exclamation,
	ExclamationEqual,
	Equal,
	PlusEqual, MinusEqual,
	DobEqual,
	
	Greater,
	GreaterEqual,
	Less,
	LessEqual,
	
	Identifier,
	String,
	Number,
	
	DobColon,
	
	If, Else,
	Function, Export, Return,
	Break, Continue,
	Exit,
	While, Do, Foreach,
	Random, Length,
	Tab,
	
	Import,
	
	EOF
}