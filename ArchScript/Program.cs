using Main.Data;
using Main.Intermediate;
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
			context.DefineDefaultGlobals();
			while (true) {
				Console.WriteLine(context.eval(Console.ReadLine()).ToString());
			}
		}
	}

	namespace Intermediate {
		class LispContext {
			public Dictionary<string, LispData> globals { get; private set; } = new Dictionary<string, LispData>();
			public void DefineDefaultGlobals() {
				DefineGlobal("add", new LispLambda(args => {
					float total = 0;
					bool integer = true;
					args.ForEach(expression => {
						LispData n = expression.eval();
						if(n is LispFloat f) {
							integer = false;
							total += f.value;
						} else if(n is LispInt i) {
							total += i.value;
						}
					});
					if(integer) {
						return new LispInt((int) total);
					} else {
						return new LispFloat(total);
					}
				}));
				DefineGlobal("setq", new LispLambda(args => {
					string key = "";
					LispExpression first = args[0];
					LispData first2 = first.eval();
					if(first is LispSymbol symbol) {
						key = symbol.symbol;
					} else if(first2 is LispString s) {
						key = s.value;
					}
					if(key.Length > 0) {
						return globals[key] = args[1].eval();
					} else {
						return new LispError("Identifier expected [" + first2 + "] ### " + " ###");
					}
				}));
				DefineGlobal("eval", new LispLambda(args => {
					LispData first = args[0].eval();
					if(first is LispString s) {
						return new LispSymbol(this, s.value).eval();
					} else {
						return new LispError("not implemented yet");
					}
				}));
			}
			private void DefineGlobal(string name, LispData value) {
				globals[name] = value;
			}
			public LispData eval(string code) {
				return new LispCode(this, code).eval();
			}
		}
		interface LispExpression {
			LispData eval();
		}

		class LispCode : LispExpression {
			private LispContext context;
			private string code;
			public LispCode(LispContext context, string code) {
				this.context = context;
				this.code = code;
			}
			public LispData eval() {
				int i = 0;
				return process(ref i).eval();
			}
			private LispExpression process(ref int index) {
				int begin = index;
				while (index < code.Length) {
					char c = code[index];
					switch (c) {
						case '"':
							index++;
							return new LispLiteral(String(ref index));
						case '\'':
							index++;
							return new LispLiteral(Literal(ref index));
						case var digit when (c >= '0' && c <= '9'):
							return new LispLiteral(LiteralNumber(ref index));
						case var letter when (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'):
							return Symbol(ref index);
						case '(':
							index++;
							return Tree(ref index);

					}
					index++;
				}
				return new LispLiteral(new LispError("unknown ### " + code.Substring(begin, index - begin) + " ###"));
			}
			/**
			 * i: index of the first character after the apostrophe
			 * Result: i set to the closing quote
			 * */
			private LispData String(ref int index) {
				int begin = index;
				StringBuilder s = new StringBuilder();
				while (index < code.Length) {
					char c = code[index];
					switch (c) {
						case '"':
							return new LispString(s.ToString());
						default:
							s.Append(c);
							break;
					}
					index++;
				}
				return new LispError("mismatched open quote ### " + code.Substring(begin, 1 + index - begin) + " ###");
			}
			private LispData Literal(ref int index) {
				int begin = index;
				switch (code[index]) {
					case '(':
						index++;
						return LiteralList(ref index);
					case var c when (c >= '0' && c <= '9'): {
						return LiteralNumber(ref index);
					}
					case ' ':
						return new LispError("bad literal ### " + code.Substring(begin, 1 + index - begin) + " ###");
					default: {
						return LiteralString(ref index);
					}
				}
			}
			/**
			 * i: index of the first character after the apostrophe
			 * Result: i set to directly after the closing parenthesis
			 * */
			private LispData LiteralList(ref int i) {
				int begin2 = i;
				List<LispData> items = new List<LispData>();
				while (i < code.Length) {
					char c = code[i];
					switch (c) {
						case ' ':
							i++;
							break;
						case ')':
							i++;
							return new LispList(items);
						default:
							items.Add(Literal(ref i));
							break;
					}
				}
				return new LispError("mismatched close paranthesis ### " + code.Substring(begin2, (i < code.Length ? 1 : 0) + i - begin2) + " ###");
			}
			/**
			 * i: index of the first character after the apostrophe
			 * Result: i set to directly after the last character
			 * */
			private LispData LiteralString(ref int i) {
				StringBuilder result = new StringBuilder();
				while (i < code.Length) {
					char c = code[i];
					if (c == ' ' || c == '(' || c == ')') {
						return new LispString(result.ToString());
					} else {
						result.Append(c);
					}
					i++;
				}
				return new LispString(result.ToString());
			}
			/**
			 * i: index of the first digit
			 * Resuult: i set to directly after the last digit
			 * */
			private LispData LiteralNumber(ref int i) {
				int begin = i;
				int n = 0;
				while (i < code.Length) {
					switch (code[i]) {
						case var c when (c >= '0' && c <= '9'):
							n = (n * 10) + (c - '0');
							break;
						case '.':
							i++;
							return LiteralFloat(ref i, n);
						case ' ':
						case '(':
						case ')':
							return new LispInt(n);
						default:
							return new LispError("Invalid number format ### " + code.Substring(begin, 1 + i - begin) + " ###");
					}
					i++;
				}
				return new LispInt(n);
			}
			/**
			 * i: index of digit directly after the decimal point
			 * value: integer part of the result
			 * Result: i set to directly after the last digit of the float
			 **/
			private LispData LiteralFloat(ref int i, int n) {
				int begin = i;
				float d = 0;
				while (i < code.Length) {
					switch (code[i]) {
						case var c when (c >= '0' && c <= '9'):
							d += (c - '0') / (10f * (1 + i - begin));
							break;
						case ' ':
						case '(':
						case ')':
							return new LispFloat(n + d);
						default:
							return new LispError("Invalid float format ### " + code.Substring(begin, 1 + i - begin) + " ###");
					}
					i++;
				}
				return new LispFloat(n + d);
			}
			/**
			 * i: the index of the first character of the symbol.
			 * Result: i should be set to the index directly after the last character of the symbol
			 */
			private LispSymbol Symbol(ref int i) {
				int begin = i;
				StringBuilder symbol = new StringBuilder();
				while (i < code.Length) {
					char c = code[i];
					switch (c) {
						case ' ':
							return new LispSymbol(context, symbol.ToString());
						default:
							i++;
							symbol.Append(c);
							break;
					}
				}
				return new LispSymbol(context, symbol.ToString());
			}
			//We use this to handle function calls
			private LispExpression Tree(ref int i) {
				int begin = i;
				List<LispExpression> items = new List<LispExpression>();
				while(i < code.Length) {
					char c = code[i];
					switch(c) {
						case ')':
							return new LispTree(context, items, code.Substring(begin, i - begin));
						case ' ':
							i++;
							break;
						default:
							items.Add(process(ref i));
							break;
					}
				}
				return new LispLiteral(new LispError("mismatched close parenthesis ### " + code.Substring(begin, i - begin) + " ###"));
			}
		}
		//Container for a literal (which always evaluates to itself)
		class LispLiteral : LispExpression {
			private LispData literal;
			public LispLiteral(LispData literal) {
				this.literal = literal;
			}
			public LispData eval() => literal;
		}
		class LispSymbol : LispExpression {
			private LispContext context;
			public string symbol { get; private set; }
			public LispSymbol(LispContext context, string symbol) {
				this.context = context;
				this.symbol = symbol;
			}
			public LispData eval() {
				return context.globals.ContainsKey(symbol) ?  context.globals[symbol] : new LispError("no binding for symbol [" + symbol + "]");
			}
		}
		class LispTree : LispExpression {
			public LispContext context { get; private set; }
			public List<LispExpression> items { get; private set; }
			private string code;    //For debug purposes only
			public LispTree(LispContext context, List<LispExpression> items, string code) {
				this.context = context;
				this.items = items;
				this.code = code;
			}
			public LispData eval() {
				var first = items[0].eval();
				if (first is LispInt i) {
					return new LispError("function name expected [" + i.value + "] ### " + code + " ###");
				} else if (first is LispFloat f) {
					return new LispError("function name expected [" + f.value + "] ### " + code + " ###");
				} else {
					LispLambda function = null;
					if (first is LispLambda l) {
						function = l;
					} else if (first is LispString s) {
						LispData binding = context.globals[s.value];
						if (binding == null) {
							return new LispError("unknown function [" + s.value + "] ### " + code + " ###");
						} else if (binding is LispLambda l2) {
							function = l2;
						} else {
							return new LispError("function name expected [" + s.value + "] ### " + code + " ###");
						}
					}
					if(function == null) {
						return new LispError("function name expected [" + first.ToString() + "] ### " + code + " ###");
					} else {
						return function.run(items.GetRange(1, items.Count - 1));
					}
				}
			}
			public string ToString() {
				string result = "LispTree[";
				foreach (LispData e in items) {
					result += e.ToString() + ", ";
				}
				result += ']';
				return result;
			}
		}
	}
	namespace Data {
		interface LispData {
			string ToString();
		}
	}
	
	class LispLambda : LispData {
		public Func<List<LispExpression>, LispData> code { get; private set; }
		public LispLambda(Func<List<LispExpression>, LispData> code) {
			this.code = code;
		}
		public LispData run(List<LispExpression> args) {
			return code.Invoke(args);
		}
	}
	class LispInt : LispData {
		public int value { get; private set; }
		public LispInt(int value) {
			this.value = value;
		}
		public string ToString() => "LispInt[" + value + "]";
	}
	class LispFloat : LispData {
		public float value { get; private set; }
		public LispFloat(float value) {
			this.value = value;
		}
		public string ToString() => "LispFloat[" + value + "]";
	}
	class LispList : LispData {
		public List<LispData> items { get; private set; }
		public LispList(List<LispData> items) {
			this.items = items;
		}
		public string ToString() {
			string result = "LispList[";
			foreach(LispData e in items) {
				result += e.ToString() + " ";
			}
			result = result.TrimEnd();
			result += ']';
			return result;
		}
	}
	class LispString : LispData {
		public string value { get; private set; }
		public LispString(string s) {
			this.value = s;
		}
		public string ToString() => "LispString[" + value + "]";
	}
	class LispError : LispData {
		public string error { get; private set; }
		public LispError(string error) {
			this.error = error;
		}
		public string ToString() => "LispError[" + error + "]";
	}
}
