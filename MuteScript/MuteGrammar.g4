grammar MuteGrammar;

compileUnit:
	moduleStatement moduleScope EOF
	;

moduleStatement:
	'module' name = ID;

moduleScope:
	(imports += moduleImportStatement)*
	(types += typeDefinitionStatement)*;

moduleImportStatement:
	'import' name = ID;

typeDefinitionStatement:
	typeAccess? classDefinition;

typeAccess:
	('public' | 'private');

classMemberAccess:
	('public' | 'private' | 'protected');

classDefinition:
	(mutable = ('mutable' | 'immutable'))? 'class' name = ID genericArguments? '{' (members += classMember)* '}';

classMember:
	classMemberAccess? (fieldClassMember | methodClassMember);

fieldClassMember:
	(storageClass = ('mutable' | 'const' | 'immutable'))? name = ID (':' type = dataType)?;

methodClassMember
	:PURE? name = 'new' arguments = tupleDefinition (statementBlock | '=>' expression)
	|PURE? DEFER name = 'new' arguments = tupleDefinition
	|PURE? name = ID genericArguments? arguments = tupleDefinition ':' returnType = dataType (statementBlock| '=>' expression)
	|PURE? DEFER name = ID genericArguments? arguments = tupleDefinition ':' returnType = dataType;
	
genericArguments:
	'<' (arguments += genericArgument ',')* (arguments += genericArgument) '>';

genericArgument:
	typeArgument
	| constArgument;

typeArgument:
	name = ID;

constArgument:
	name = ID ':' constExpression;

methodParameter:
	name = ID ':' type = dataType;

dataType:
	(typeReference
	| funcDefinition
	| tupleDefinition
	| builtInType
	| rangeType) (nullable = '?')?;

rangeType:
	'[' dataType ']';
builtInType:
	'int'
	| 'bool'
	| 'float'
	| 'string'
	| 'vec2'
	| 'vec3'
	| 'vec4'
	| 'mat4'
	| 'quat'
	| 'void'
	| 'never';

typeReference:
	(module = ID '.')? name = ID genericArguments?;

funcDefinition:
	'fn' tupleDefinition;


tupleDefinition:
	'(' ((members += tupleMember ',')* (members += tupleMember))? ')';

tupleMember:
	storageClass = ('mutable'|'immutable'|'const')? ((name = ID ':' type = dataType) | type = dataType | name = ID '<-' expression);


/* Expressions */

statementBlock:
	'{' (statements += expression)* '}';

variableDefExpression:
	storageClass = ('mutable'|'const'|'immutable') name = ID ( ':' dataType '<-' expression | ':' dataType| '<-' expression)?;


expression:
	 memberAccessExpression | primaryExpression;
/*	|   indexerExpression
    |   methodCallExpression
	|   'new' creator
	|   powExpression
    |   multiplicativeExpression
    |   additiveExpression
    |   comparisonExpression
    |   equivalenceExpression
    |   expression '&' expression
    |   expression '^' expression
    |   expression '|' expression
    |   expression '&&' expression
    |   expression '||' expression
    |   expression '?' expression ':' expression
    |   assignmentExpression;*/

memberAccessExpression: memberAccessExpression operator = '.' memberName = ID
	| indexerExpression;

indexerExpression: indexerExpression ('[' expression ']' | '.' constInteger)
	| methodCallExpression;

methodCallExpression: methodCallExpression operator = '!' expression
	| powExpression;

powExpression: powExpression operator = '^' expression
	| multiplicativeExpression;

multiplicativeExpression: multiplicativeExpression operator = ('*'|'/'|'%') expression
	| additiveExpression;

additiveExpression: additiveExpression operator = ('+'|'-') expression
	| comparisonExpression;

comparisonExpression: comparisonExpression operator = ('<=' | '>=' | '>' | '<') expression
	| equivalenceExpression;

equivalenceExpression: equivalenceExpression operator = ('=' | '<>') expression
	| assignmentExpression;

assignmentExpression: <assoc=right> primaryExpression operator = '<-' expression
	| primaryExpression;

primaryExpression:
	variableExpression |
	tupleExpression |
	constExpression;

tupleExpression: '(' (arguments += expression (',' arguments += expression ',')*)? ')';

variableExpression: name = ID;

constExpression : constInteger | constString;
constString: STRING;
constInteger: INT;

LINE_COMMENT: '//' .*? '\r'? ('\n' | EOF) -> skip; 
BLOCK_COMMENT: '/*' .*? '*/' -> skip;
STRING: '"' STRING_CHAR*? '"';
fragment STRING_CHAR: 
	.
	| EscapeSequence;

fragment EscapeSequence:
	'\\' ['"?abfnrtv\\];


WS: [ \t]+ -> skip;
//IGNORE: [;] -> skip;
NL: ('\n' | '\r' '\n') -> skip;

MUL_OPERATORS: ('*' | '/' | '%');
ADD_OPERATORS: ('+' | '-');
ID_HEAD: ('a' .. 'z')|('A' .. 'Z')|'_';
ID_TAIL: ('a' .. 'z')|('A' .. 'Z')|('0' .. '9')|'_';
ID: ID_HEAD ID_TAIL*;
INT: ('1' .. '9')('0' .. '9')*|'0';
FLOAT: '-'?(INT '.' INT|'.'INT);

PURE: 'pure';
DEFER: 'defer';