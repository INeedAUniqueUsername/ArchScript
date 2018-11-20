/**
 * ArchScript implementation
 * The behavior of ArchScript is based on George Moromisato's TLisp
 * */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Main.LispCheckedPrimitive;
using static Main.LispExpression;

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
				try {
					Console.WriteLine(interpreter.Parse().Eval(context).Source);
				} catch(LispError e) {
					Console.WriteLine(e.Message);
				}
				
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
			for (int i = stack.Count - 1; i > -1; i--) {
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
		public void SetLocal(string symbol, LispData data) {
			stack.Last()[symbol] = data;
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
		public void Push() {
			stack.Add(new Dictionary<string, LispData>());
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

					LispData condition = new LispNil();
					LispData then = new LispNil();
					LispData branch = new LispNil();
					switch(args.Count) {
						case 0:
							throw new LispError("condition expected");
						case 1:
							throw new LispError("then expression expected");
						case 2:
							condition = args[0];
							then = args[1];
							break;
						case 3:
							condition = args[0];
							then = args[1];
							branch = args[2];
							break;
						default:
							throw new LispError("too many arguments");
					}

					if(condition.Eval(context) is LispNil) {
						return branch.Eval(context);
					} else {
						return then.Eval(context);
					}
				})},
				{"while", new LispCheckedPrimitive(new List<ArgTypes>() {ArgTypes.Unevaluated, ArgTypes.Unevaluated}, (context, args) => {
					LispData result = new LispNil();
					while(!(args[0].Eval(context) is LispNil))
						result = args[1].Eval(context);
					return result;
				})},
				{"read", new LispCheckedPrimitive(new List<ArgTypes>(), (context, args) => {
					return new LispString(Console.ReadLine());
				}) },
				{"print", new LispCheckedPrimitive(new List<ArgTypes>() {ArgTypes.Any}, (context, args) => {
					Console.WriteLine(args[0].Source);
					return new LispTrue();
				})},
				{"eval", new LispCheckedPrimitive(new List<ArgTypes>() {ArgTypes.Any}, (context, args) => {
					LispData arg = args[0];

					if(arg is LispString s)
						return new LispParser(s.value).Parse().Eval(context);
					return arg.Eval(context);
				})},
				{"setq", new LispCheckedPrimitive(new List<ArgTypes>() {ArgTypes.Symbol, ArgTypes.Any}, (context, args) => {
					variables.Set(args[0].Source, args[1]);
					return args[1];
				})},
				{"add", new LispCheckedPrimitive(new List<ArgTypes>() { ArgTypes.Rest }, (context, args) => {
					double result = 0;
					bool integer = true;
					args.ForEach(a => {
						if(a is LispDouble d) {
							integer = false;
							result += d.AsDouble();
						} else if(a is LispInteger i) {
							result += i.AsInt();
						} else {
							throw new LispError($"Number expected {a.Source}");
						}
					});
					if(integer)
						return new LispInteger((int) result);
					else
						return new LispDouble(result);
				})},
				{"cat", new LispCheckedPrimitive(new List<ArgTypes>() { ArgTypes.Rest }, (context, args) => {
					return new LispString(string.Join("", args.Select(a => a is LispString s ? s.value : a.Source)));
				})},
				{"int", new LispCheckedPrimitive(new List<ArgTypes>() { ArgTypes.Any }, (context, args) => {
					LispData arg = args[0];
					if(arg is LispString s) {
						if(new LispParser((s).value).Parse() is LispInteger i)
							return i;
					} else if(arg is LispNumber n) {
						return new LispInteger(n.AsInt());
					}
					return new LispNil();
				}) },
				{"throw", new LispCheckedPrimitive(new List<ArgTypes>() { ArgTypes.String }, (context, args) => {
					LispString s = args[0] as LispString;
					throw new LispError(s.value);
				})},
				{"try", new LispCheckedPrimitive(new List<ArgTypes>() { ArgTypes.Unevaluated, ArgTypes.Symbol, ArgTypes.Unevaluated }, (context, args) => {
					try {
						return args[0].Eval(context);
					} catch(LispError e) {
						variables.Push();
						variables.SetLocal(args[1].Source, e);

						LispData result = args[2].Eval(context);
						variables.Pop();
						return result;
					}
				})},
			};
		}
	}
	interface LispFunction : LispData {
		LispData Run(LispContext context, List<LispData> args);
	}
	/*
	class LispPrimitive : LispData {

	}
	*/
	class LispPrimitive : LispFunction {
		public string Source => "[LispPrimitive]";
		Func<LispContext, List<LispData>, LispData> func;
		public LispPrimitive(Func<LispContext, List<LispData>, LispData> func) {
			this.func = func;
		}
		public LispData Eval(LispContext context) => this;
		public LispData Run(LispContext context, List<LispData> args) {
			return func(context, args);
		}
		public override string ToString() => "[LispPrimitive]";
	}
	class LispCheckedPrimitive : LispFunction {
		public string Source => "LispPrimitive";

		public enum ArgTypes {
			Function,
			Integer,
			Double,
			List,
			Number,
			Symbol,
			String,
			Unevaluated,
			Any,
			Struct,
			Rest
		}
		List<ArgTypes> argtypes;
		Func<LispContext, List<LispData>, LispData> func;
		public LispCheckedPrimitive(List<ArgTypes> argtypes, Func<LispContext, List<LispData>, LispData> func) {
			this.argtypes = argtypes;
			this.func = func;
		}
		public LispData Eval(LispContext context) => this;
		private void Validate(LispContext context, List<LispData> args) {
			bool rest = false;
			for(int i = 0; i < args.Count; i++) {

				if(i > argtypes.Count - 1) {
					if(rest) {
						EvaluateArg();
					} else {
						throw new LispError("Too many arguments");
					}
				} else {
					switch(argtypes[i]) {
						case ArgTypes.Function:
							if(!(EvaluateArg() is LispFunction)) {
								throw new LispError("Function expected");
							}
							break;
						case ArgTypes.Integer:
							if (!(EvaluateArg() is LispInteger)) {
								throw new LispError("Integer expected");
							}
							break;
						case ArgTypes.Double:
							if (!(EvaluateArg() is LispDouble)) {
								throw new LispError("Double expected");
							}
							break;
						case ArgTypes.List:
							if (!(EvaluateArg() is LispList)) {
								throw new LispError("List expected");
							}
							break;
						case ArgTypes.Number:
							if (!(EvaluateArg() is LispNumber)) {
								throw new LispError("Number expected");
							}
							break;
						case ArgTypes.Symbol:
							if (GetArg() is LispString str)
								SetArg(new LispSymbol(str.value));
							if (!(GetArg() is LispSymbol)) {
								throw new LispError("Symbol expected");
							}
							break;
						case ArgTypes.String:
							if (!(EvaluateArg() is LispString)) {
								throw new LispError("String expected");
							}
							break;
						case ArgTypes.Unevaluated:
							break;
						case ArgTypes.Any:
							EvaluateArg();
							break;
						case ArgTypes.Struct:
							if (!(EvaluateArg() is LispStruct)) {
								throw new LispError("Struct expected");
							}
							break;
						case ArgTypes.Rest:
							EvaluateArg();
							rest = true;
							break;
					}
				}
				void SetArg(LispData arg) {
					args[i] = arg;
				}
				LispData GetArg() {
					return args[i];
				}
				LispData EvaluateArg() {
					return (args[i] = args[i].Eval(context));
				}
			}
		}
		public LispData Run(LispContext context, List<LispData> args) {
			Validate(context, args);
			return func(context, args);
		}
		public override string ToString() => "[LispPrimitive]";
	}
	class LispError : Exception, LispData {
		public new string Source => Message;

		public LispError(string Message) : base(Message) {
		}

		public LispData Eval(LispContext context) => this;
	}
	class LispNil : LispData {
		public string Source => "Nil";

		public LispData Eval(LispContext context) => this;
		public override string ToString() => "[LispNil]";
	}
	class LispTrue : LispData {
		public string Source => "True";

		public LispData Eval(LispContext context) => this;
		public override string ToString() => "[LispTrue]";
	}
	public interface LispData {
		string Source {
			get;
		}
		LispData Eval(LispContext context);
	}
	public interface LispNumber : LispData {
		double AsDouble();
		int AsInt();
		LispData Eval(LispContext context);
	}
	public class LispDouble : LispNumber {
		public string Source => $"{value}";
		double value;
		public LispDouble(double value) {
			this.value = value;
		}

		public double AsDouble() => value;
		public int AsInt() => (int)value;
		public LispData Eval(LispContext context) => this;
		public override string ToString() {
			return $"[LispDouble {value}]";
		}
	}
	public class LispInteger : LispNumber {
		public string Source => $"{value}";

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
		public string Source => $"\"{value}\"";
		public string value;
		public LispString(string value) {
			this.value = value;
		}
		public LispData Eval(LispContext context) => this;
		public LispData Symbol(LispContext context) => new LispSymbol(value).Eval(context);
		public override string ToString() => $"[LispString \"{value}\"]";
	}
	public class LispStruct : LispData {
		public string Source => $"{{ {string.Join(", ", value.Select(pair => $"{pair.Key}:{pair.Value.Source}"))} }}";
		public Dictionary<string, LispData> value;
		public LispStruct(Dictionary<string, LispData> value) {
			this.value = value;
		}
		public LispData Eval(LispContext context) => this;
		public override string ToString() => $"[LispStruct {Source}]";
	}
	public class LispList : LispData {
		public string Source => $"(list {string.Join(" ", value.ConvertAll(d => d.Source))})";
		private List<LispData> value;
		public LispList(List<LispData> value) {
			this.value = value;
		}
		public LispData Eval(LispContext context) => this;
		public override string ToString() {
			return $"[LispList ({string.Join(" ", value.ConvertAll(d => d.Source))})]";
		}
	}
	class LispExpression : LispData {
		public string Source => $"({string.Join(" ", subexpressions.ConvertAll(d => d.Source))})";
		public List<LispData> subexpressions;
		public LispExpression(List<LispData> subexpressions) {
			this.subexpressions = subexpressions;
		}
		public LispData Eval(LispContext context) {
			try {
				LispData func = subexpressions.First().Eval(context);
				var args = subexpressions.GetRange(1, subexpressions.Count - 1);
				if (func is LispFunction f)
					return f.Run(context, args);
				else if (func is LispString s && s.Symbol(context) is LispFunction f2)
					return f2.Run(context, args);
				else
					throw new LispError($"function expected ### {func.Source}");
			} catch (LispError e) {
				throw new LispError($"{e.Message} ### {Source}");
			}
		}
		public override string ToString() {
			return $"[LispExpression ({string.Join(" ", subexpressions.ConvertAll(d => d.ToString()))})]";
		}
		public class LispSymbol : LispData {
			public string Source => Symbol;
			public string Symbol;
			public LispSymbol(string Symbol) {
				this.Symbol = Symbol;
			}

			public LispData Eval(LispContext context) {
				if (context.variables.Lookup(Symbol, out LispData result))
					return result;
				else
					throw new LispError($"No binding for symbol [{Source}]");
			}
			public override string ToString() {
				return $"[LispSymbol {Symbol}]";
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

		public class LispParser {

			string code;
			int index, column, row;


			public LispParser(string code) {
				this.code = code;
				index = 0;
				column = 0;
				row = 0;
			}
			public LispData Parse() {
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
			public LispData ParseExpression() {
				List<LispData> subexpressions = new List<LispData>();

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
							throw new LispError($"Missing close paren ### {code.Substring(begin, index - begin)} ### {code} ###");
						default:
							subexpressions.Add(Parse());
							break;
					}
				}

				return new LispExpression(subexpressions);
			}
			public LispData ParseSymbol() {
				string symbol = GetCurrentChar().ToString();
				UpdateIndex(symbol);
				bool active = true;
				while (active) {
					Token t = Read();
					switch (t.type) {
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
				while (active) {
					Token t = Read();
					switch (t.type) {
						case TokenType.Quote:
							active = false;
							UpdateIndex(t.str);
							break;
						case TokenType.End:
							throw new LispError($"Unexpected end of line ### {code.Substring(begin, index - begin)} ### {code} ###");
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
				while (active) {
					Token t = Read();
					switch (t.type) {
						case TokenType.Digit:
							if (dot) {
								result += (t.str[0] - '0') / place;
								place *= 10d;
							} else {
								result = result * 10 + (t.str[0] - '0');
							}

							UpdateIndexOnce();
							break;
						case TokenType.Dot:
							if (dot) {
								throw new LispError($"Invalid double format ### {code.Substring(begin, index)} ### {code} ###");
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
							throw new LispError($"Invalid number format ### {code.Substring(begin, index - begin)} ### {code} ###");
					}
				}

				Done:
				if (dot)
					return new LispDouble(result);
				else
					return new LispInteger((int)result);
			}
			public LispData ParseQuoted() {
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
							throw new LispError($"Unknown quote type ### {code.Substring(begin, index - begin)} ### {code} ###");
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
									throw new LispError($"Unknown quote type ### {code.Substring(begin, index - begin)} ### {code} ###");
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
									throw new LispError($"Unknown quote type ### {code.Substring(begin, index - begin)} ### {code} ###");
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
				if (code.Length == index) {
					return;
				} else if (code[index] == '\n') {
					column = 0;
					row++;
				}
				index++;
				column++;
			}
			private void UpdateIndex(string s) {
				foreach (char c in s) {
					if (c == '\n') {
						column = 0;
						row++;
					} else {
						column++;
					}
					index++;
				}
			}
			public void RevertIndex(string s) {
				foreach (char c in s.Reverse()) {
					if (c == '\n') {
						column = 0;
						while (code[index - (column + 1)] != '\n') {
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
}