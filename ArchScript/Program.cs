/**
 * ArchScript implementation
 * The behavior of ArchScript is based on George Moromisato's TLisp
 * */

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
		static class Help {
			public static string GetSource(this List<LispExpression> args, string name) {
				List<LispExpression> tree = new List<LispExpression>();
				tree.Add(new LispSymbol(null, name));
				tree.AddRange(args);
				return new LispTree(null, tree).GetSource();
			}
		}
		class LispContext {
			public Dictionary<string, LispData> globals { get; private set; } = new Dictionary<string, LispData>();
			public Dictionary<string, string> help { get; private set; } = new Dictionary<string, string>();
			public void DefineDefaultGlobals() {
				//Need to deal with error passing and parameter type checking

				new Dictionary<string, LispData> {
					{ "add", new LispPrimitive(args => {
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
					})}, {"block", new LispPrimitive(args => {
						LispExpression localsArg = args[0];
						Dictionary<string, LispData> locals = new Dictionary<string, LispData>();
						if(localsArg is LispLiteral literal && literal.eval().IsNil()) {

						} else if(localsArg is LispTree tree) {
							//for locals, we accept a list of names or names plus initial values
							foreach(LispExpression local in tree.items) {
								if(local is LispSymbol symbol) {
									locals.Add(symbol.symbol, LispNil.Nil);
								} else if(local is LispTree treeLocal && treeLocal.items.Count == 2 && treeLocal.items[0] is LispSymbol symbolLocal) {
									locals.Add(symbolLocal.symbol, treeLocal.items[1].eval());
								} else {
									return new LispError("invalid block local initializer ### " + local.GetSource() + " ###");
								}
							}
						}

						Dictionary<string, LispData> localsPrevious = DefineLocals(locals);

						LispData result = LispNil.Nil;
						foreach(LispExpression statement in args.GetRange(1, args.Count - 1)) {
							result = statement.eval();
							switch(result) {
								case LispError e:
									result = e;
									goto Exit;
								case LispBreak b:
									result = b.result;
									goto Exit;
								case LispReturn r:
									result = r.result;
									goto Exit;
							}
						}
						Exit:
						UndefineLocals(localsPrevious);
						return result;
					})}, {"break", new LispPrimitive(args => {
						//Only works if returned in block or loop; undefined behavior anywhere else
						return new LispBreak(args.Count == 0 ? LispNil.Nil : args[0].eval());
					})},{"eval", new LispPrimitive(args => {
						LispData first = args[0].eval();
						if(first is LispString s) {
							return new LispSymbol(this, s.value).eval();
						}
						return new LispError("not implemented yet");
					})}, {"help", new LispPrimitive(args => {
						if(args.Count == 0) {
							StringBuilder result = new StringBuilder();
							help.Values.ToList().ForEach(text => result.Append(text));
							return new LispString(result.ToString());
						} else {
							StringBuilder result = new StringBuilder();
							string key = args[0].eval().AsString();
							help.ToList().ForEach(pair => {
								if (pair.Key.StartsWith(key))
									result.Append(pair.Value);
							});
							return new LispString(result.ToString());
						}
					})},{ "if", new LispPrimitive(args => {
						
						return	args.Count > 1
								? (!args[0].eval().IsNil())
									? args[1].eval()
									: (args.Count > 2)
										? args[2].eval()
										: LispNil.Nil
								: new LispError("then expression expected ### " + args.GetSource("if") + " ###");
					})},{"lambda", new LispPrimitive(args => {
						LispExpression pars = args[0];
						Console.WriteLine(pars.GetSource());
						LispExpression code = args[1];
						if (pars is LispLiteral literal && literal.eval().IsNil()) {
							return new LispLambda(this, new List<string>(), code.GetSource());
						}
						List<string> argsResult = new List<string>();
						if(pars is LispTree tree) {
							bool correct = true;
							tree.items.ForEach(item => {
								if(item is LispSymbol symbol) {
									argsResult.Add(symbol.symbol);
								} else {
									correct = false;
								}
							});
							if(correct) {
								return new LispLambda(this, argsResult, code);
							} else {
								return new LispError("parameter list expected ### " + pars.GetSource() + " ###");
							}
						}
						return new LispError("Not implemented yet");
					})}, {"return", new LispPrimitive(args => {
						//Only works if returned in block or loop or lambda; undefined behavior anywhere else
						return new LispReturn(args.Count == 0 ? LispNil.Nil : args[0].eval());
					})}, {"setq", new LispPrimitive(args => {
						string key = "";
						LispExpression first = args[0];
						LispData first2 = first.eval();
						if (first is LispSymbol symbol) {
							key = symbol.symbol;
						} else if (first2 is LispString s) {
							key = s.value;
						}
						if (key.Length > 0) {
							return globals[key] = args[1].eval();
						} else {
							return new LispError("Identifier expected [" + first2 + "] ### " + " ###");
						}
					})}, {"struct", new LispPrimitive(args => {
						if(args.Count%2 == 1)
							return new LispError("insufficient arguments ### " + new LispTree(this, args).GetSource() + " ###");
						Dictionary<string, LispData> properties = new Dictionary<string, LispData>();
						for(int i = 0; i < args.Count; i += 2) {
							LispData keyData = args[i].eval();
							string key = keyData.AsString();
							if(key.Length == 0 && !(keyData is LispString))
								return new LispError("invalid key [" + keyData.ToString() + "] ### " + args.GetSource("struct") + " ###");

							LispData value = args[i+1].eval();
							properties[key] = value;
						}
						return new LispStruct(properties);
					})}, {"set@", new LispPrimitive(args => {
						LispData target = args[0].eval();

						if(target is LispStruct s) {

							LispData keyData = args[1].eval();
							string key = keyData.AsString();
							if(key.Length == 0 && !(keyData is LispString))
								return new LispError("invalid key [" + keyData.ToString() + "] ### " + args.GetSource("struct") + " ###");

							LispData value = args[2].eval();
							s.properties[key] = value;
							return s;
						} else if(target is LispList l) {
							LispData indexData = args[1].eval();
							if(indexData is LispNumber n) {
								int index = n.AsInt();
								if(index >= l.items.Count) {
									return new LispError("index out of range [" + index + "] ### " + args.GetSource("set@") + " ###");
								}
								LispData value = args[2].eval();
								l.items[index] = value;
								return l;
							} else {
								return new LispError("int expected [" + indexData.ToString() + "] ### " + args.GetSource("set@") + " ###");
							}
						} else {
							return new LispError("struct or list expected [" + target.ToString() + "] ### " + args.GetSource("set@") + " ###");
						}
					})}
				}.ToList().ForEach(x => DefineGlobal(x.Key, x.Value));
				DefineAlias("setq", "=");

				new Dictionary<string, string> {
					{ "@", "(@ struct|list key|index) -> item at key|index of struct|list" }
				}.ToList().ForEach(x => help[x.Key] = x.Value);
			}
			public void DefineAlias(string source, string target) {
				globals[target] = globals[source];
			}
			public void DefineGlobal(string name, LispData value) {
				globals[name] = value;
			}
			public Dictionary<string, LispData> DefineLocals(Dictionary<string, LispData> locals) {
				Dictionary<string, LispData> previous = new Dictionary<string, LispData>();
				foreach (var pair in locals.ToList()) {
					string key = pair.Key;
					LispData value = pair.Value;

					previous[key] = globals.ContainsKey(key) ? globals[key] : null;
					globals[key] = value;
				}
				return previous;
			}
			public void UndefineLocals(Dictionary<string, LispData> previous) {
				foreach(var pair in previous.ToList()) {
					string key = pair.Key;
					LispData value = pair.Value;

					if(value == null) {
						globals.Remove(key);
					} else {
						globals[key] = value;
					}
				}
			}
			public LispData eval(string code) {
				return new LispCode(this, code).eval();
			}
		}
		interface LispExpression {
			LispData eval();
			string GetSource();
			string ToString();
		}

		class LispCode : LispExpression {
			private LispContext context;
			public string source { get; private set; }
			(int row, int column) pos;

			LispExpression result;
			public LispCode(LispContext context, string code) {
				this.context = context;
				this.source = code;
				pos = (0, 0);
				result = process();
			}
			public LispData eval() => result.eval();
			private void updatePos(char c) {
				if (c == '\n') {
					pos = (pos.row + 1, 0);
				} else {
					pos.column++;
				}
			}
			private LispExpression process() {
				int i = 0;
				return process(ref i);
			}
			private LispExpression process(ref int index) {
				int begin = index;
				StringBuilder local = new StringBuilder();
				while (index < source.Length) {
					char c = source[index];
					local.Append(c);
					updatePos(c);
					switch (c) {
						case '"':
							index++;
							return new LispLiteral(String(ref index));
						case '\'':
							index++;
							return new LispLiteral(Literal(ref index));
						case var digit when (c >= '0' && c <= '9'):
							return new LispLiteral(LiteralNumber(ref index));
						case '(':
							index++;
							return Tree(ref index);
						case ' ':
							break;
						//case var letter when (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'):
						default:
							return Symbol(ref index);
					}
					index++;
				}
				return new LispLiteral(new LispError("unknown ### " + local.ToString() + " ###"));
			}
			/**
			 * i: index of the first character after the apostrophe
			 * Result: i set to the closing quote
			 * */
			private LispData String(ref int index) {
				int begin = index;
				StringBuilder s = new StringBuilder();
				StringBuilder local = new StringBuilder();
				while (index < source.Length) {
					char c = source[index];
					local.Append(c);
					updatePos(c);
					switch (c) {
						case '"':
							index++;
							return new LispString(s.ToString());
						default:
							s.Append(c);
							break;
					}
					index++;
				}
				return new LispError("mismatched open quote ### " + local.ToString() + " ###");
			}
			/**
			 * Call this function when you encounter an apostrophe literal. We determine which type of literal we have.
			 * */
			private LispData Literal(ref int index) {
				char c = source[index];
				updatePos(c);
				switch (c) {
					case '(':
						index++;
						return LiteralList(ref index);
					case var c2 when (c2 >= '0' && c2 <= '9'): {
						return LiteralNumber(ref index);
					}
					case ' ':
						return new LispError("bad literal ### " + source[index] + " ###");
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
				StringBuilder local = new StringBuilder();
				List<LispData> items = new List<LispData>();
				while (i < source.Length) {
					char c = source[i];
					local.Append(c);
					updatePos(c);
					switch (c) {
						case ' ':
							i++;
							break;
						case ')':
							i++;
							if (items.Count > 0)
								return new LispList(items);
							else
								return LispNil.Nil;			//Empty list becomes Nil
						default:
							items.Add(Literal(ref i));
							break;
					}
				}
				return new LispError("mismatched close parenthesis ### " + local.ToString() + " ###");
			}
			/**
			 * i: index of the first character after the apostrophe
			 * Result: i set to directly after the last character
			 * */
			private LispData LiteralString(ref int i) {
				StringBuilder result = new StringBuilder();
				while (i < source.Length) {
					char c = source[i];
					updatePos(c);
					if (c == ' ' || c == '(' || c == ')') {
						goto Exit;
					} else {
						result.Append(c);
					}
					i++;
				}
				Exit:
				return new LispString(result.ToString());
			}
			/**
			 * i: index of the first digit
			 * Resuult: i set to directly after the last digit
			 * */
			private LispData LiteralNumber(ref int i) {
				int begin = i;
				int n = 0;
				StringBuilder local = new StringBuilder();
				while (i < source.Length) {
					char c = source[i];
					local.Append(c);
					updatePos(c);
					switch (c) {
						case var d when (d >= '0' && d <= '9'):
							n = (n * 10) + (c - '0');
							break;
						case '.':
							i++;
							return LiteralFloat(ref i, n);
						case ' ':
						case '(':
						case ')':
							goto Exit;
						default:
							return new LispError("Invalid number format ### " + local.ToString() + " ###");
					}
					i++;
				}
				Exit:
				return new LispInt(n);
			}
			/**
			 * i: index of digit directly after the decimal point
			 * value: integer part of the result
			 * Result: i set to directly after the last digit of the float
			 **/
			private LispData LiteralFloat(ref int i, int n) {
				StringBuilder local = new StringBuilder();
				int begin = i;
				float d = 0;
				while (i < source.Length) {
					char c = source[i];
					local.Append(c);
					updatePos(c);
					switch (c) {
						case var c2 when (c >= '0' && c <= '9'):
							d += (c - '0') / (10f * (1 + i - begin));
							break;
						case ' ':
						case '(':
						case ')':
							goto Exit;
						default:
							return new LispError("Invalid float format ### " + local.ToString() + " ###");
					}
					i++;
				}

				Exit:
				return new LispFloat(n + d);
			}
			/**
			 * i: the index of the first character of the symbol.
			 * Result: i should be set to the index directly after the last character of the symbol
			 */
			private LispExpression Symbol(ref int i) {
				int begin = i;
				StringBuilder symbol = new StringBuilder();
				while (i < source.Length) {
					char c = source[i];
					updatePos(c);
					switch (c) {
						case '(':
						case ')':
						case ' ':
							goto Exit;
						default:
							i++;
							symbol.Append(c);
							break;
					}
				}

				Exit:
				switch(symbol.ToString()) {
					case "Nil":
						return new LispLiteral(LispNil.Nil);
					case "True":
						return new LispLiteral(LispTrue.True);
					default:
						return new LispSymbol(context, symbol.ToString());
				}
			}
			//We use this to handle function calls
			private LispExpression Tree(ref int i) {
				int begin = i;
				List<LispExpression> items = new List<LispExpression>();
				while(i < source.Length) {
					char c = source[i];
					updatePos(c);
					switch (c) {
						case ')':
							//return new LispTree(context, items);
							i++;
							if (items.Count > 0)
								return new LispTree(context, items);
							else
								return new LispLiteral(LispNil.Nil);
						case ' ':
							i++;
							break;
						default:
							items.Add(process(ref i));
							break;
					}
				}
				return new LispLiteral(new LispError("mismatched close parenthesis ### " + source.Substring(begin, i - begin) + " ###"));
			}
			public string GetSource() => source;
		}
		//Container for a literal (which always evaluates to itself)
		class LispLiteral : LispExpression {
			private LispData literal;
			public LispLiteral(LispData literal) {
				this.literal = literal;
			}
			public LispData eval() => literal;
			public string GetSource() => literal.AsString();
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
			public string GetSource() => symbol;
		}
		class LispTree : LispExpression {
			public LispContext context { get; private set; }
			public List<LispExpression> items { get; private set; }
			public LispTree(LispContext context, List<LispExpression> items) {
				this.context = context;
				this.items = items;
			}
			public LispData eval() {
				var first = items[0].eval();
				if (first is LispInt i) {
					return new LispError("function name expected [" + i.value + "] ### " + GetSource() + " ###");
				} else if (first is LispFloat f) {
					return new LispError("function name expected [" + f.value + "] ### " + GetSource() + " ###");
				} else {
					LispFunction function = null;
					if (first is LispFunction l) {
						function = l;
					} else if (first is LispString s) {
						LispData binding = context.globals.ContainsKey(s.value) ? context.globals[s.value] : null;
						if (binding == null) {
							return new LispError("unknown function [" + s.value + "] ### " + GetSource() + " ###");
						} else if (binding is LispFunction l2) {
							function = l2;
						} else {
							return new LispError("function name expected [" + s.value + "] ### " + GetSource() + " ###");
						}
					}
					if(function == null) {
						return new LispError("function name expected [" + first.ToString() + "] ### " + GetSource() + " ###");
					} else {
						return function.run(items.GetRange(1, items.Count - 1));
					}
				}
			}
			public string GetSource() {
				StringBuilder result = new StringBuilder("(");
				items.ForEach(exp => result.Append(exp.GetSource() + " "));
				return result.ToString().TrimEnd() + ")";
			}
			public override string ToString() {
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
		static class SData {
			public static LispLiteral ToLiteral(LispData data) {
				return new LispLiteral(data);
			}
		}
		interface LispData {
			bool IsNil();
			string AsString();
			string ToString();
		}

		class LispError : LispData {
			public string error { get; private set; }
			public LispError(string error) {
				this.error = error;
			}
			public bool IsNil() => false;
			public string AsString() => "";
			public override string ToString() => "LispError[" + error + "]";
		}
		class LispFloat : LispData {
			public float value { get; private set; }
			public LispFloat(float value) {
				this.value = value;
			}
			public float AsFloat() => value;
			public int AsInt() => (int)value;
			public bool IsNil() => false;
			public string AsString() => value.ToString();
			public override string ToString() => "LispFloat[" + value + "]";
		}
		interface LispFunction : LispData {
			LispData run(List<LispExpression> args);
		}
		enum TypeArgs {
			Boolean,
			Error,
			Float,
			Int,
			List,
			Number,
			String,
			Struct,
			Symbol,
			Unevaluated,
			Any,
			AnyNonerror,
			RestNonerror
		}
		static class LispFunctionHelper {
			public static bool matches(this TypeArgs type, LispExpression e, out LispData result) {
				result = null;
				return false;
			}
		}
		class LispInt : LispData, LispNumber {
			public int value { get; private set; }
			public LispInt(int value) {
				this.value = value;
			}
			public float AsFloat() => value;
			public int AsInt() => value;
			public bool IsNil() => false;
			public string AsString() => value.ToString();
			public override string ToString() => "LispInt[" + value + "]";
		}
		class LispLambda : LispFunction {
			private LispContext context;
			private List<string> pars;
			private LispExpression code;
			private string source;
			public LispLambda(LispContext context, List<string> args, string code) {
				this.context = context;
				this.pars = args;
				this.code = new LispCode(context, code);
			}
			public LispLambda(LispContext context, List<string> args, LispExpression code) {
				this.context = context;
				this.pars = args;
				this.code = code;
			}
			public bool IsNil() => false;
			public string AsString() => "[lambda expression]";
			public override string ToString() => "[lambda expression]";
			public LispData run(List<LispExpression> args) {
				int initialized = Math.Min(args.Count, pars.Count);

				Dictionary<string, LispData> previousGlobals = new Dictionary<string, LispData>();

				for (int i = 0; i < pars.Count; i++) {
					string par = pars[i];
					if(context.globals.ContainsKey(par)) {
						previousGlobals[par] = context.globals[par];
					}
				}

				for (int i = 0; i < initialized; i++) {
					context.DefineGlobal(pars[i], args[i].eval());
				}
				for (int i = initialized; i < pars.Count; i++) {
					context.DefineGlobal(pars[i], LispNil.Nil);
				}

				LispData result = code.eval();

				//Unbox return statement here
				if (result is LispReturn r)
					result = r.result;

				for (int i = 0; i < pars.Count; i++) {
					string par = pars[i];
					if(context.globals.ContainsKey(par)) {
						context.DefineGlobal(par, previousGlobals[par]);
					} else {
						context.globals.Remove(par);
					}
					
				}
				return result;
			}
		}
		class LispList : LispData {
			public List<LispData> items { get; private set; }
			public LispList(List<LispData> items) {
				this.items = items;
			}
			public bool IsNil() => false;
			public string AsString() => "";
			public override string ToString() {
				string result = "LispList[";
				foreach (LispData e in items) {
					result += e.ToString() + " ";
				}
				result = result.TrimEnd();
				result += ']';
				return result;
			}
		}
		class LispNil : LispData {
			public static readonly LispNil Nil = new LispNil();
			private LispNil() { }
			public bool IsNil() => true;
			public string AsString() => "Nil";
			public override string ToString() => AsString();
		}
		interface LispNumber {
			float AsFloat();
			int AsInt();
		}
		class LispPrimitive : LispFunction {
			public Func<List<LispExpression>, LispData> code { get; private set; }
			public List<TypeArgs> typeArgs { get; private set; }

			public LispPrimitive(Func<List<LispExpression>, LispData> code, List<TypeArgs> typeArgs = null) {
				this.code = code;
				this.typeArgs = typeArgs;
			}
			public bool IsNil() => false;
			public string AsString() => "[lambda expression]";
			public LispError checkArgs(List<LispExpression> args) {
				if (typeArgs == null)
					return null;
				for(int i = 0; i < args.Count; i++) {
					TypeArgs type;
					LispExpression arg = args[i];
					if(i < typeArgs.Count) {
						type = typeArgs[i];
						//When we evaluate the argument, we replace its list item with a literal
						switch(type) {
						}
					} else {
						type = typeArgs[typeArgs.Count - 1];
						switch(type) {
							case TypeArgs.RestNonerror:
								if (arg is LispError e)
									return e;
								break;
							default:
								return new LispError("Too many arguments [" + arg.GetSource() + "] ### ###");
						}
					}
				}
				return null;
			}
			public LispData run(List<LispExpression> args) {
				LispError error = checkArgs(args);
				if (error != null)
					return error;
				return code.Invoke(args);
			}
		}
		class LispString : LispData {
			public string value { get; private set; }
			public LispString(string s) {
				this.value = s;
			}
			public bool IsNil() => false;
			public string AsString() => value;
			public override string ToString() => "LispString[" + value + "]";
		}
		class LispStruct : LispData {
			public Dictionary<string, LispData> properties { get; private set; }
			public LispStruct(Dictionary<string, LispData> properties) {
				this.properties = properties;
			}

			public bool IsNil() => false;
			public string AsString() => "";
			public override string ToString() {
				string result = "{ ";
				properties.ToList().ForEach(pair => result += pair.Key + ":" + pair.Value + " ");
				result += "}";
				return result;
			}
		}
		class LispTrue : LispData {
			public static readonly LispTrue True = new LispTrue();
			private LispTrue() { }
			public bool IsNil() => false;
			public string AsString() => "True";
			public override string ToString() => AsString();
		}
		class LispBreak : LispData {
			public LispData result { get; private set; }
			public LispBreak(LispData result) => this.result = result;
			public string AsString() => result.AsString();
			public bool IsNil() => result.IsNil();
		}
		class LispReturn : LispData {
			public LispData result { get; private set; }
			public LispReturn(LispData result) => this.result = result;
			public string AsString() => result.AsString();
			public bool IsNil() => result.IsNil();
		}
	}
}
