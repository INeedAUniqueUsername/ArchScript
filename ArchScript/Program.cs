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
			LispContext context = new LispContext();
			while (true) {
				LispParser interpreter = new LispParser(Console.ReadLine());
				Console.WriteLine(interpreter.Parse().Eval(context));
			}
		}
	}
	static class Helper {
		public static bool StartsWithAt(this string source, string substring, int index) {
			return source.Substring(index, substring.Length).Equals(substring);
		}
		public static bool EndsWithAt(this string source, string substring, int index) {
			return source.Substring(index - substring.Length, substring.Length).Equals(substring);
		}
	}
	public class StackDictionary<T, K> {
		public Dictionary<string, LispData> globals;
		public List<Dictionary<string, LispData>> stack;

		public StackDictionary() {
			globals = new Dictionary<string, LispData>();
			stack = new List<Dictionary<string, LispData>>();
		}

		public bool Lookup(string symbol, out LispData result) {
			for (int i = stack.Count - 1; i > -1; i++) {
				if (stack[i].TryGetValue(symbol, out result)) {
					return true;
				}
			}
			if (globals.TryGetValue(symbol, out result)) {
				return true;
			}
			return false;
		}
		public void Set(string symbol, LispData data) {
			for (int i = stack.Count - 1; i > -1; i++) {
				if (stack[i].ContainsKey(symbol)) {
					stack[i][symbol] = data;
					return;
				}
			}
			globals[symbol] = data;
		}
		public void Delete(string symbol) {
			for (int i = stack.Count - 1; i > -1; i++) {
				if (stack[i].ContainsKey(symbol)) {
					stack[i].Remove(symbol);
					return;
				}
			}
			globals.Remove(symbol);
		}
		public void Push(Dictionary<string, LispData> locals) {
			stack.Add(locals);
		}
		public void Pop() {
			stack.RemoveAt(stack.Count - 1);
		}
	}
	public class LispContext {
		public StackDictionary<string, LispData> variables { get; private set; }
		public LispContext() {
			variables = new StackDictionary<string, LispData>();
			variables.globals = new Dictionary<string, LispData>() {
				{"if", new LispPrimitive((context, args) => {

					LispCode condition = new LispNil();
					LispCode then = new LispNil();
					LispCode @else = new LispNil();
					if(args.Count == 0)
						throw new Exception("Too few arguments");
					if(args.Count >= 1)
						condition = args[0];
					if(args.Count >= 2)
						then = args[1];
					if(args.Count >= 3)
						@else = args[2];
					if(args.Count >= 4)
						throw new Exception("Too many arguments");

					if(condition.Eval(context) is LispNil) {
						return @else.Eval(context);
					} else {
						return then.Eval(context);
					}
				}) },

			};
		}
	}
	interface LispFunction : LispData {
		LispData Run(LispContext context, List<LispCode> args);
	}
	/*
	class LispPrimitive : LispData {

	}
	*/
	class LispPrimitive : LispFunction {
		Func<LispContext, List<LispCode>, LispData> func;
		public LispPrimitive(Func<LispContext, List<LispCode>, LispData> func) {
			this.func = func;
		}
		public LispData Eval(LispContext context) => this;
		public LispData Run(LispContext context, List<LispCode> args) {
			return func(context, args);
		}
		public override string ToString() => "[LispPrimitive]";
	}
	class LispNil : LispData, LispCode {
		public LispData Eval(LispContext context) => this;
		public override string ToString() => "[LispNil]";
	}
	class LispTrue : LispData, LispCode {
		public LispData Eval(LispContext context) => this;
		public override string ToString() => "[LispTrue]";
	}
	public interface LispData : LispCode {

	}
	public interface LispNumber : LispData {
		double AsDouble();
		int AsInt();
		LispData Eval(LispContext context);
	}
	public class LispDouble : LispNumber {
		double value;
		public LispDouble(double value) {
			this.value = value;
		}

		public double AsDouble() => value;

		public int AsInt() => (int) value;
		public LispData Eval(LispContext context) => this;
		public override string ToString() {
			return $"[LispDouble {value}]";
		}
	}
	public class LispInteger : LispNumber {
		int value;
		public LispInteger(int value) {
			this.value = value;
		}
		public double AsDouble() => value;
		public int AsInt() => value;
		public LispData Eval(LispContext context) => this;
		public override string ToString() {
			return $"[LispInt {value}]";
		}
	}
	public class LispString : LispData {
		public string value;
		public LispString(string value) {
			this.value = value;
		}
		public LispData Eval(LispContext context) => this;
		public override string ToString() => $"[LispString \"{value}\"]";
	}
	public class LispList : LispData {
		private List<LispData> value;
		public LispList(List<LispData> value) {
			this.value = value;
		}
		public LispData Eval(LispContext context) => this;
		public override string ToString() {
			string result = "[LispList (";
			value.GetRange(0, value.Count - 1).ForEach(s => result += s.ToString() + " ");
			result += value.Last().ToString();
			result += ")]";
			return result;
		}
	}
	public interface LispCode {
		LispData Eval(LispContext context);
	}
	class LispExpression : LispCode {
		public List<LispCode> subexpressions;
		public LispExpression(List<LispCode> subexpressions) {
			this.subexpressions = subexpressions;
		}
		public LispData Eval(LispContext context) {
			LispData func = subexpressions.First().Eval(context);
			var args = subexpressions.GetRange(1, subexpressions.Count - 1);
			if (func is LispFunction f)
				return f.Run(context, args);
			else if (func is LispString s && context.variables.Lookup(s.value, out LispData v) && v is LispFunction f2)
				return f2.Run(context, args);
			else
				throw new Exception("function expected");
		}
		public override string ToString() {
			string result = "[LispExpression (";
			subexpressions.GetRange(0, subexpressions.Count - 1).ForEach(s => result += s.ToString() + " ");
			result += subexpressions.Last().ToString();
			result += ")]";
			return result;
		}
	}
	class LispSymbol : LispCode {
		public string symbol;
		public LispSymbol(string symbol) {
			this.symbol = symbol;
		}
		public LispData Eval(LispContext context) {
			if (context.variables.Lookup(symbol, out LispData result))
				return result;
			throw new Exception($"No binding for symbol ### {symbol} ### {symbol} ###");
		}
		public override string ToString() {
			return $"[LispSymbol {symbol}]";
		}
	}
	/*
	class LispLiteral {
		public LispData data;
		public LispLiteral(LispData data) {
			this.data = data;
		}
		public override string ToString() {
			return $"[LispLiteral {data.ToString()}]";
		}
	}
	*/

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
			switch (t.type) {
				case TokenType.OpenParen:
					UpdateIndex(t.str);
					return ParseExpression();
				case TokenType.CloseParen:

					break;
				case TokenType.Dot:
					break;
				case TokenType.Letter:
					return ParseSymbol();
				case TokenType.Digit:
					return ParseNumber();
				case TokenType.Apostrophe:
					return ParseQuoted();
				case TokenType.Quote:
					return ParseString();
				case TokenType.Space:
					UpdateIndex(t.str);
					break;
				case TokenType.Unknown:
					UpdateIndex(t.str);
					break;
				case TokenType.End:

					break;
			}
			return new LispNil();
		}
		public LispCode ParseExpression() {
			List<LispCode> subexpressions = new List<LispCode>();

			int begin = index;
			bool active = true;
			while (active) {
				Token t = Read();
				switch (t.type) {
					case TokenType.CloseParen:
						active = false;
						UpdateIndex(t.str);
						break;
					case TokenType.Space:
						UpdateIndex(t.str);
						break;
					case TokenType.End:
						throw new Exception($"Missing close paren ### {code.Substring(begin, index - begin)} ### {code} ###");
					default:
						subexpressions.Add(Parse());
						break;
				}
			}

			return new LispExpression(subexpressions);
		}
		public LispCode ParseSymbol() {
			string symbol = GetCurrentChar().ToString();
			UpdateIndex(symbol);
			bool active = true;
			while(active) {
				Token t = Read();
				switch(t.type) {
					case TokenType.Letter:
					case TokenType.Digit:
						symbol += t.str;
						UpdateIndex(t.str);
						break;
					default:
						active = false;
						break;
				}
			}
			if (symbol.ToUpper() == "NIL")
				return new LispNil();
			else if (symbol.ToUpper() == "TRUE")
				return new LispTrue();
			return new LispSymbol(symbol);
		}
		public LispString ParseString() {
			int begin = index;
			string result = "";
			UpdateIndex(Read().str);
			bool active = true;
			while(active) {
				Token t = Read();
				switch(t.type) {
					case TokenType.Quote:
						active = false;
						UpdateIndex(t.str);
						break;
					case TokenType.End:
						throw new Exception($"Unexpected end of line ### {code.Substring(begin, index - begin)} ### {code} ###");
					default:
						result += t.str;
						UpdateIndex(t.str);
						break;
				}
			}
			return new LispString(result);
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
					case TokenType.CloseParen:
					case TokenType.Space:
					case TokenType.End:
						active = false;
						break;
					default:
						throw new Exception($"Invalid number format ### {code.Substring(begin, index - begin)} ### {code} ###");
				}
			}

			Done:
			if (dot)
				return new LispDouble(result);
			else
				return new LispInteger((int) result);
		}
		public LispCode ParseQuoted() {
			int begin = index;
			UpdateIndex(Read().str);
			return ParseSubQuoted();

			LispData ParseSubQuoted() {
				Token t = Read();
				switch (t.type) {
					case TokenType.Letter:
						return ParseQuotedSymbol();
					case TokenType.Digit:
						return ParseNumber();
					case TokenType.Space:
						return new LispNil();
					case TokenType.OpenParen:
						return ParseQuotedList();
					default:
						throw new Exception($"Unknown quote type ### {code.Substring(begin, index - begin)} ### {code} ###");
				}
				LispData ParseQuotedSymbol() {
					bool active = true;
					string symbol = "";
					while (active) {
						Token t2 = Read();
						switch (t2.type) {
							case TokenType.Letter:
							case TokenType.Digit:
								symbol += t.str;
								UpdateIndex(t.str);
								break;
							case TokenType.End:
							case TokenType.Space:
							case TokenType.CloseParen:
								active = false;
								break;
							default:
								throw new Exception($"Unknown quote type ### {code.Substring(begin, index - begin)} ### {code} ###");
						}
					}
					return new LispString(symbol);
				}
				LispList ParseQuotedList() {
					UpdateIndex(Read().str);
					bool active = true;
					List<LispData> value = new List<LispData>();
					while (active) {
						Token t2 = Read();
						switch (t2.type) {
							case TokenType.OpenParen:
							case TokenType.Letter:
							case TokenType.Digit:
								value.Add(ParseSubQuoted());
								break;
							case TokenType.Space:
								UpdateIndex(t.str);
								break;
							case TokenType.End:
							case TokenType.CloseParen:
								UpdateIndex(t.str);
								active = false;
								break;
							default:
								throw new Exception($"Unknown quote type ### {code.Substring(begin, index - begin)} ### {code} ###");
						}
					}
					return new LispList(value);
				}
			}
		}
		
		/*
		private LispString ParseString() {

		}
		*/
		private char GetCurrentChar() {
			return code[index];
		}
		private void UpdateIndexOnce() {
			if(code.Length == index) {
				return;
			} else if (code[index] == '\n') {
				column = 0;
				row++;
			}
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
		public void RevertIndex(string s) {
			foreach(char c in s.Reverse()) {
				if(c == '\n') {
					column = 0;
					while(code[index - (column + 1)] != '\n') {
						column++;
					}
					row--;
				} else {
					column--;
				}
				index--;
			}
		}
		public Token Read() {
			Token result;
			if (code.Length == index) {
				result = new Token(TokenType.End, "");
			} else if (code.StartsWithAt("(", index)) {
				result = new Token(TokenType.OpenParen, "(");
			} else if (code.StartsWithAt(")", index)) {
				result = new Token(TokenType.CloseParen, ")");
			} else if (code.StartsWithAt("'", index)) {
				result = new Token(TokenType.Apostrophe, "'");
			} else if (code.StartsWithAt("\"", index)) {
				result = new Token(TokenType.Quote, "\"");
			} else if (code.StartsWithAt(" ", index) || code.StartsWithAt("\n", index) || code.StartsWithAt("\t", index)) {
				result = new Token(TokenType.Space, code[index].ToString());
			} else if (code.StartsWithAt(".", index)) {
				result = new Token(TokenType.Dot, ".");
			} else if ((code[index] >= 'a' && code[index] <= 'z') || (code[index] >= 'A' && code[index] <= 'Z')) {
				result = new Token(TokenType.Letter, code[index].ToString());
			} else if (code[index] >= '0' && code[index] <= '9') {
				result = new Token(TokenType.Digit, code[index].ToString());
			} else {
				result = new Token(TokenType.Unknown, code[index].ToString());
			}
			//UpdateIndex(result.str);
			return result;
		}
		public Token Unread() {
			Token result;
			if (code.Length == 0) {
				result = new Token(TokenType.End, "");
			} else if (code.EndsWithAt("(", index)) {
				result = new Token(TokenType.OpenParen, "(");
			} else if (code.EndsWithAt(")", index)) {
				result = new Token(TokenType.CloseParen, ")");
			} else if (code.EndsWithAt("'", index)) {
				result = new Token(TokenType.Apostrophe, "'");
			} else if (code.EndsWithAt("\"", index)) {
				result = new Token(TokenType.Quote, "\"");
			} else if (code.EndsWithAt(" ", index) || code.StartsWithAt("\n", index) || code.StartsWithAt("\t", index)) {
				result = new Token(TokenType.Space, code[index].ToString());
			} else if (code.EndsWithAt(".", index)) {
				result = new Token(TokenType.Dot, ".");
			} else if ((code[index] >= 'a' && code[index] <= 'z') || (code[index] >= 'A' && code[index] <= 'Z')) {
				result = new Token(TokenType.Letter, code[index].ToString());
			} else if (code[index] >= '0' && code[index] <= '9') {
				result = new Token(TokenType.Digit, code[index].ToString());
			} else {
				result = new Token(TokenType.Unknown, code[index].ToString());
			}
			//RevertIndex(result.str);
			return result;
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
