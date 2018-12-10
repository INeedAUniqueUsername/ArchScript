# ArchScript

## Classes
  - `StackDictionary`: A data structure consisting of a base `globals` dictionary and a stack of `locals` dictionaries that can be pushed and popped to represent local variable frames on the call stack. When setting values, we first check if the key exists as a `local` variable and set it as one if it is (otherwise it is global). Same thing for getting values.
  - `LispContext`: Represents the universe in which an expression is executed. Has a `StackDictionary` member and initializes the default global functions.
  - `LispData`: In Lisp, everything is data; code and values can be treated the same.
    - `LispPrimitive`: A function object to be executed in Lisp code.
  	- `LispCheckedPrimitive`: A function object that checks (and usually pre-evaluates) its arguments before executing to ensure that the arguments are proper.
  	- `LispError`: An error that may be thrown while executing Lisp code. It is usually caught by the outer context and then rethrown with additional contextual information.
  	- `LispNil`: A sinle value that represents false and null.
  	- `LispTrue`: A single that represents anything other than Nil.
	- `LispNumber`: A data value representing a float or integer.
	  - `LispDouble`: Container for a double value
	  - `LispInteger`: Container for an integer value
	- `LispString`: A sequence of characters enclosed in quotes
	- `LispStruct`: An object composed of key and value pairs (each separated by a colon) enclosed in curly braces
	- `LispList`: A sequence of data values not intended to be executed like code
	- `LispInterrupt`: An object meant to interrupt the current control flow in a `block` or any kind of loop.
	  - `LispReturn`: Stores a value and indicates that the current block or loop should stop executing and return the value. Can be nested to exit multiple nested blocks or loops. Cannot be handled by any other functions.
	  - `LispLabel`: Represents a point in a block to which the current program execution can be reverted to via `goto`. When received, the block stores the index of the subexpression from which this label was received. Cannot be handled by any other functions.
	  - `LispGoto`: Stores the name of a label and indicates that the outer block should go to the label. When received, the block looks up the index of the label (if received already) and jumps back to that index if present, otherwise returns the `LispGoto` object to the enclosing block or loop.
	- `LispExpression`: Represents an expression of a function and its inputs to be executed within a given `LispContext`.
	- `LispSymbol`: A variable to be evaluated to its actual value in a given context. As of recent changes, a `LispSymbol` is also capable of setting its own value and providing access to struct members in its context.
	- `LispParser`: Handles string code and parses it into `LispData` by reading individual tokens. It tracks the current index and unclosed structures, and returns errors with contextual information upon encountering invalid code. It may also request additional input instead of throwing errors.
## Functions
