/**
 * ArchScript implementation
 * The behavior of ArchScript is based on George Moromisato's TLisp
 * */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Main {
	class Program {
		static void Main(string[] args) {
			Program p = new Program();
			p.run();
		}
		private void run() {
			while (true) {
				LispParser interpreter = new LispParser(Console.ReadLine());
				Console.WriteLine(interpreter.Parse().ToString());
			}
		}
	}
	static class Helper {
		public static bool StartsWithAt(this string source, string substring, int index) {
			return source.Substring(index, substring.Length).Equals(substring);
		}
	}
	class LispContext {
		public string eval(string code) {
			return new LispParser(code).Parse().ToString();
		}
	}
	class LispNil: LispData, LispCode {
		public LispData Eval() => this;
		public override string ToString() => "[LispNil]";
	}
	class LispTrue : LispData, LispCode {
		public LispData Eval() => this;
		public override string ToString() => "[LispTrue]";
	}
	public interface LispData {

	}
	interface LispNumber : LispData, LispCode {
		double AsDouble();
		int AsInt();
		LispData Eval();
	}
	class LispDouble : LispNumber {
		double value;
		public LispDouble(double value) {
			this.value = value;
		}

		public double AsDouble() => value;

		public int AsInt() => (int) value;
		public LispData Eval() => this;
		public override string ToString() {
			return $"[LispDouble {value}]";
		}
	}
	class LispInteger : LispNumber {
		int value;
		public LispInteger(int value) {
			this.value = value;
		}
		public double AsDouble() => value;
		public int AsInt() => value;
		public LispData Eval() => this;
		public override string ToString() {
			return $"[LispInt {value}]";
		}
	}
	public class LispString : LispData, LispCode {
		private string value;
		public LispString(string value) {
			this.value = value;
		}
		public LispData Eval() => this;
		public override string ToString() => $"[LispString {value}";
	}
	interface LispCode {
		LispData Eval();
	}
	class LispLiteral {
		public LispData data;
		public LispLiteral(LispData data) {
			this.data = data;
		}
		public override string ToString() {
			return $"[LispLiteral {data.ToString()}]";
		}
	}

	class LispParser {

		string code;
		int index, column, row;
		

		public LispParser(string code) {
			this.code = code;
			index = 0;
			column = 0;
			row = 0;
		}
		public LispCode Parse() {
			Token t = Read();
			switch(t.type) {
				case TokenType.OpenParen:

					break;
				case TokenType.CloseParen:

					break;
				case TokenType.Dot:
					break;
				case TokenType.Letter:
					break;
				case TokenType.Digit:
					return ParseNumber();
				case TokenType.Quote:
					//ParseString();
					break;
				case TokenType.Space:

					break;
				case TokenType.Unknown:

					break;
				case TokenType.End:

					break;
			}
			return new LispNil();
		}
		public LispNumber ParseNumber() {
			int begin = index;
			double result = GetCurrentChar() - '0';
			bool dot = false;
			double place = 10d;
			UpdateIndexOnce();
			bool active = true;
			while(active) {
				Token t = Read();
				switch(t.type) {
					case TokenType.Letter:
						UpdateIndexOnce();
						throw new Exception($"Invalid number format ### {code.Substring(begin, index)} ### {code} ###");
					case TokenType.Digit:
						if(dot) {
							result += (t.str[0] - '0') / place;
							place *= 10d;
						} else {
							result = result * 10 + (t.str[0] - '0');
						}
						
						UpdateIndexOnce();
						break;
					case TokenType.Dot:
						if(dot) {
							throw new Exception($"Invalid double format ### {code.Substring(begin, index)} ### {code} ###");
						} else {
							dot = true;
						}
						UpdateIndexOnce();
						break;
					case TokenType.Space:
					case TokenType.End:
						goto Done;
				}
			}

			Done:
			if (dot)
				return new LispDouble(result);
			else
				return new LispInteger((int) result);
		}
		/*
		private LispString ParseString() {

		}
		*/
		private char GetCurrentChar() {
			return code[index];
		}
		private void UpdateIndexOnce() {
			if (code[index] == '\n')
				row++;
			index++;
			column++;
		}
		private void UpdateIndex(string s) {
			foreach(char c in s) {
				if(c == '\n') {
					column = 0;
					row++;
				} else {
					column++;
				}
				index++;
			}
		}

		public Token Read() {
			if (code.Length == index)
				return new Token(TokenType.End, "");
			else if (code.StartsWithAt("(", index))
				return new Token(TokenType.OpenParen, "(");
			else if (code.StartsWithAt(")", index))
				return new Token(TokenType.CloseParen, ")");
			else if (code.StartsWithAt("'", index))
				return new Token(TokenType.Apostrophe, "'");
			else if (code.StartsWithAt("\"", index))
				return new Token(TokenType.Quote, "\"");
			else if (code.StartsWithAt(" ", index) || code.StartsWithAt("\n", index) || code.StartsWithAt("\t", index))
				return new Token(TokenType.Space, code[index].ToString());
			else if (code.StartsWithAt(".", index))
				return new Token(TokenType.Dot, ".");
			else if ((code[index] >= 'a' && code[index] <= 'z') || (code[index] >= 'A' && code[index] <= 'Z'))
				return new Token(TokenType.Letter, code[index].ToString());
			else if (code[index] >= '0' && code[index] <= '9')
				return new Token(TokenType.Digit, code[index].ToString());
			else
				return new Token(TokenType.Unknown, code[index].ToString());
		}
		public enum TokenType {

			OpenParen,
			CloseParen,
			Apostrophe,
			Quote,
			Space,
			Dot,
			Letter,
			Digit,
			Unknown,
			End
		}
		public class Token {
			public readonly string str;
			public readonly TokenType type;
			public Token(TokenType type, string str) {
				this.str = str;
				this.type = type;
			}
		}

	}
}
