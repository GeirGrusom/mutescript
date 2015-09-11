using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using MuteScript.ParseTree;
using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace MuteScript
{
    public static class EqualityMethods
    {
        public static bool Equals(Terminal term, string value)
        {
            if (term == null)
                return ReferenceEquals(term, value);
            return term.Equals(value);
        }
    }

    public static class ContextExtensions
    {
        public static SourcePositionInfo GetPosition(this ParserRuleContext context, string filename = "")
        {
            if (context == null)
                return null;
            return new SourcePositionInfo(filename, context.Start.Line, context.Start.Column, context.Stop.StartIndex - context.Start.StopIndex);
        }

        public static SourcePositionInfo GetPosition(this IToken context, string filename = "")
        {

            if (context == null)
                return null;
            return new SourcePositionInfo(filename, context.Line, context.Column, context.StartIndex - context.StopIndex);
        }

        public static ParseException CreateParseException(this ParserRuleContext context, string filename = "")
        {
            return new ParseException(new SourcePositionInfo(filename, context.Start.Line, context.Start.Column, context.stop.StopIndex - context.Start.StartIndex));
        }

        public static ParseException CreateParseException(this ParserRuleContext context, string message, string filename = "")
        {
            return new ParseException(message, new SourcePositionInfo(filename, context.Start.Line, context.Start.Column, context.stop.StopIndex - context.Start.StartIndex));
        }

        public static Terminal GetTerminal(this IToken context, string filename = "")
        {
            if (context == null)
                return null;
            return new Terminal(context.GetPosition(filename), context.Text);
        }

        public static Terminal GetTerminal(this ITerminalNode node, string filename = "")
        {
            return node.Symbol.GetTerminal();
        }
    }


    public class MuteScriptParser : MuteGrammarBaseVisitor<Node>
    {
        public Node Parse(string input)
        {
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream(input));
            var parser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));
            return VisitCompileUnit(parser.compileUnit());
        }
        public override Node VisitCompileUnit([NotNull] MuteGrammarParser.CompileUnitContext context)
        {
            var mod = context.moduleStatement();
            var scope = context.moduleScope();
            string moduleName = mod?.name.Text;
            var types = scope?._types.Cast<MuteGrammarParser.TypeDefinitionStatementContext>().Select(x => VisitTypeDefinitionStatement(x));

            return new ParseTree.Module(context.GetPosition(),
                moduleName, 
                scope._imports?.Select(Visit) ?? Enumerable.Empty<Node>(), 
                types ?? Enumerable.Empty<Node>());
        }

        public override Node VisitTypeDefinitionStatement([NotNull] MuteGrammarParser.TypeDefinitionStatementContext context)
        {
            var def = context.classDefinition();
            var typeAccess = context.typeAccess();
            var access = typeAccess != null ? (Terminal)Visit(context.typeAccess()) : null;
            return VisitClassDefinition(access, def);
        }

        public override Node VisitTypeAccess([NotNull] MuteGrammarParser.TypeAccessContext context)
        {
            if (context == null)
                return null;
            return new Terminal(context.GetPosition(), context.GetText());
        }
        public Node VisitClassDefinition(Terminal access, [NotNull] MuteGrammarParser.ClassDefinitionContext context)
        {
            var className = context.name.GetTerminal();

            var storageClass = context.mutable.GetTerminal();

            var members = context._members.Select(Visit);

            var genericArguments = context.genericArguments()?._arguments.Select(Visit) ?? Enumerable.Empty<Node>();

            return new Class(context.GetPosition(), access, storageClass, className, null, members, genericArguments);
        }

        public override Node VisitClassMember([NotNull] MuteGrammarParser.ClassMemberContext context)
        {
            var memberAccess = context.classMemberAccess();
            Terminal access;
            if (memberAccess != null)
                access = (Terminal)VisitClassMemberAccess(memberAccess);
            else
                access = null;

            var field = context.fieldClassMember();
            if (field != null)
                return VisitFieldClassMember(access, field);
            var method = context.methodClassMember();
            if (method != null)
                return VisitMethodClassMember(access, method);
            throw new InvalidCastException();
        }

        public override Node VisitClassMemberAccess([NotNull] MuteGrammarParser.ClassMemberAccessContext context)
        {
            return new Terminal(context.GetPosition(), context.GetText());
        }

        public Node VisitFieldClassMember(Terminal access, [NotNull] MuteGrammarParser.FieldClassMemberContext context)
        {
            var name = context.name.GetTerminal();
            var storageClass = context.storageClass.GetTerminal();

            return new Field(context.GetPosition(), access, storageClass, name, Visit(context.dataType()), null);
        }

        public override Node VisitTupleDefinition([NotNull] MuteGrammarParser.TupleDefinitionContext context)
        {
            var members = context._members.Select(Visit);

            return new ParseTree.Tuple(context.GetPosition(), members);
        }

        public override Node VisitTupleMember([NotNull] MuteGrammarParser.TupleMemberContext context)
        {
            var e = context.expression();

            Expression exp = e != null ? (Expression)VisitExpression(e) : null;

            return new TupleMember(context.GetPosition(), null, context.storageClass.GetTerminal(), context.name.GetTerminal(), Visit(context.dataType()), exp);
        }



        public override Node VisitDataType([NotNull] MuteGrammarParser.DataTypeContext context)
        {
            Node type;
            var range = context.rangeType();
            var builtIn = context.builtInType();
            var funcDef = context.funcDefinition();
            var tuple = context.tupleDefinition();
            var typeRef = context.typeReference();
            if (range != null)
                type = Visit(range);
            else if (builtIn != null)
                type = Visit(builtIn);
            else if (funcDef != null)
                type = Visit(funcDef);
            else if (tuple != null)
                type = Visit(tuple);
            else if (typeRef != null)
                type = Visit(typeRef);
            else throw context.CreateParseException();
            return new DataType(context.GetPosition(), type, context.nullable.GetTerminal());
        }

        public override Node VisitBuiltInType([NotNull] MuteGrammarParser.BuiltInTypeContext context)
        {
            return context.start.GetTerminal();
        }

        public override Node VisitTypeReference([NotNull] MuteGrammarParser.TypeReferenceContext context)
        {
            var arguments = context.genericArguments();
            IEnumerable<Node> genericArguments;
            if (arguments != null)
            {
                genericArguments = arguments._arguments.Select(Visit);
            }
            else
            {
                genericArguments = Enumerable.Empty<Node>();
            }
            return new TypeReference(context.GetPosition(), context.module?.GetTerminal(), context.name.GetTerminal(), genericArguments);
        }

        

        public override Node VisitMethodClassMember([NotNull] MuteGrammarParser.MethodClassMemberContext context)
        {
            return VisitMethodClassMember(null, context);
        }

        public Node VisitMethodClassMember(Terminal access, [NotNull] MuteGrammarParser.MethodClassMemberContext context)
        {
            var name = context.name.GetTerminal();
            var gen = context.genericArguments();
            var pure = context.PURE()?.GetTerminal();
            IEnumerable<Node> genericArguments;
            if (gen != null)
                genericArguments = context.genericArguments()._arguments.Select(Visit);
            else
                genericArguments = Enumerable.Empty<Node>();
            
            var args = (ParseTree.Tuple)Visit(context.arguments);
            Node body;
            var expression = context.expression();
            var block = context.statementBlock();
            if (expression != null)
                body = VisitExpression(expression);
            else if (block != null)
                body = VisitStatementBlock(block);
            else if (context.DEFER() != null)
                body = null;
            else
                throw new FormatException();

            return new Method(context.GetPosition(), access, pure, name, genericArguments, args, body);
        }

        public override Node VisitComparisonExpression([NotNull] MuteGrammarParser.ComparisonExpressionContext context)
        {
            var right = (Expression)VisitPowExpression(context.powExpression());

            var leftExp = context.comparisonExpression();
            if (leftExp == null)
                return right;

            var left = (Expression)VisitComparisonExpression(leftExp);

            return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
        }
        public override Node VisitConditionalExpression([NotNull] MuteGrammarParser.ConditionalExpressionContext context)
        {
            var condition = (Expression)VisitTupleExpression(context.tupleExpression());

            var isTrue = (Expression)VisitExpression(context.expression());

            var isFalse = (Expression)VisitConditionalExpression(context.conditionalExpression());

            return new ConditionalExpression(context.GetPosition(), null, condition, isTrue, isFalse);
        }

        public override Node VisitPostfixExpression([NotNull] MuteGrammarParser.PostfixExpressionContext context)
        {
            var primaryExp = context.primaryExpression();

            if (primaryExp != null)
                return VisitPrimaryExpression(primaryExp);

            var left = (Expression)VisitPostfixExpression(context.postfixExpression());

            if(context.@operator != null)
            {
                return new BinaryExpression(context.GetPosition(), null, left, new MemberExpression(context.memberName.GetPosition(), null, context.memberName.GetTerminal()), context.@operator.GetTerminal());
            }

            var exp = context.expression();

            if(exp != null)
            {
                return new IndexerExpression(context.GetPosition(), left, (Expression)VisitExpression(exp));
            }

            return new IndexerExpression(context.GetPosition(), left, (Expression)VisitConstInteger(context.constInteger()));

        }

        public override Node VisitUnaryExpression([NotNull] MuteGrammarParser.UnaryExpressionContext context)
        {
            var postfix = context.postfixExpression();

            if (postfix != null)
                return VisitPostfixExpression(postfix);

            var op = context.@operator.GetTerminal();

            if(op.Value == "new")
            {
                throw new NotSupportedException();
            }

            var exp = (Expression)VisitUnaryExpression(context.unaryExpression());

            return new UnaryExpression(context.GetPosition(), exp, op);
        }

        public override Node VisitEquivalenceExpression([NotNull] MuteGrammarParser.EquivalenceExpressionContext context)
        {
            var right = (Expression)VisitComparisonExpression(context.comparisonExpression());

            var leftExp = context.equivalenceExpression();

            if (leftExp == null)
                return right;

            var left = (Expression)VisitEquivalenceExpression(leftExp);

            return new BinaryExpression(context.GetPosition(), TypeReference.Boolean, left, right, context.@operator.GetTerminal());
        }

        public override Node VisitAssignmentExpression([NotNull] MuteGrammarParser.AssignmentExpressionContext context)
        {
            var logicalOr = context.logicalOrExpression();

            if (logicalOr != null)
                return VisitLogicalOrExpression(logicalOr);

            var cond = context.conditionalExpression();
            if (cond != null)
                return VisitConditionalExpression(cond);

            var left = (Expression)VisitUnaryExpression(context.unaryExpression());

            var rightExp = context.assignmentExpression();

            if (rightExp == null)
                return left;

            var right = (Expression)VisitAssignmentExpression(rightExp);

            return new BinaryExpression(context.GetPosition(), right.Type, left, right, context.@operator.GetTerminal());
        }

        public override Node VisitAdditiveExpression([NotNull] MuteGrammarParser.AdditiveExpressionContext context)
        {
            var right = (Expression)VisitMethodCallExpression(context.methodCallExpression());

            var leftExp = context.additiveExpression();

            if (leftExp == null)
                return right;

            var left = (Expression)VisitAdditiveExpression(leftExp);

            return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
        }

        public override Node VisitMultiplicativeExpression([NotNull] MuteGrammarParser.MultiplicativeExpressionContext context)
        {
            var right = (Expression)VisitAdditiveExpression(context.additiveExpression());
            var leftExp = context.multiplicativeExpression();

            if (leftExp == null)
                return right;

            var left = (Expression)VisitMultiplicativeExpression(leftExp);

            return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
        }

        public override Node VisitExpression([NotNull] MuteGrammarParser.ExpressionContext context)
        {
            var assignment = context.assignmentExpression();
            
            return VisitAssignmentExpression(assignment);
        }

        public override Node VisitConstInteger([NotNull] MuteGrammarParser.ConstIntegerContext context)
        {
            return new ConstIntegerExpression(context.GetPosition(), context.INT().GetTerminal());
        }

        public override Node VisitConstExpression([NotNull] MuteGrammarParser.ConstExpressionContext context)
        {
            var integer = context.constInteger();
            if (integer != null)
                return VisitConstInteger(integer);
            throw new NotImplementedException();
        }

        public override Node VisitPrimaryExpression([NotNull] MuteGrammarParser.PrimaryExpressionContext context)
        {

            var constant = context.CONSTANT();

            if(constant != null)
            {
                var term = constant.GetTerminal();
                Node dataType;
                switch(term.Value)
                {
                    case "false":
                    case "true":
                        dataType = TypeReference.Boolean;
                        break;
                    case "null":
                        dataType = TypeReference.Null;
                        break;
                    case "never":
                        dataType = TypeReference.Never;
                        break;
                    case "void":
                        dataType = TypeReference.Void;
                        break;
                    default:
                        throw new NotSupportedException();
                }
                return new ConstantExpression(context.GetPosition(), dataType, term);
            }

            var integer = context.INT();

            if(integer != null)
            {
                return new ConstantExpression(context.GetPosition(), TypeReference.Int, integer.GetTerminal());
            }

            var str = context.STRING();

            if(str != null)
            {
                return new ConstantExpression(context.GetPosition(), TypeReference.String, str.GetTerminal());
            }

            var id = context.ID();

            if (id != null)
                return new SymbolExpression(context.GetPosition(), id.GetTerminal());

            var tuple = context.tupleExpression();
            if (tuple != null)
                return VisitTupleExpression(tuple);

            throw new NotSupportedException();
        }

        public override Node VisitVariableExpression([NotNull] MuteGrammarParser.VariableExpressionContext context)
        {
            var variableId = context.ID();

            return new SymbolExpression(context.GetPosition(), variableId.GetTerminal());
        }

        public override Node VisitMethodCallExpression([NotNull] MuteGrammarParser.MethodCallExpressionContext context)
        {
            var right = (Expression)VisitUnaryExpression(context.unaryExpression());

            var leftExp = context.methodCallExpression();
            if (leftExp == null)
                return right;

            var left = (Expression)VisitMethodCallExpression(leftExp);

            return new BinaryExpression(context.GetPosition(), null, left, right, context.@operator.GetTerminal());
        }

        public override Node VisitPowExpression([NotNull] MuteGrammarParser.PowExpressionContext context)
        {
            var right = (Expression)VisitMultiplicativeExpression(context.multiplicativeExpression());

            var leftExp = context.powExpression();
            if (leftExp == null)
                return right;

            var left = (Expression)VisitPowExpression(leftExp);
            
            return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
        }

        public override Node VisitTupleExpression([NotNull] MuteGrammarParser.TupleExpressionContext context)
        {
            var values = context._arguments.Select(Visit).Cast<Expression>().ToArray();
            if (values.Length == 1)
                return values[0];
            return new TupleExpression(context.GetPosition(), new ParseTree.Tuple(SourcePositionInfo.Empty, values.Select(x => x.Type)), values);
       }
    }
}
