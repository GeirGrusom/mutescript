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
            return new TupleMember(context.GetPosition(), null, context.storageClass.GetTerminal(), context.name.GetTerminal(), Visit(context.dataType()), (ParseTree.Expression)Visit(context.expression()));
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

        public override Node VisitAdditiveExpression([NotNull] MuteGrammarParser.AdditiveExpressionContext context)
        {
            var right = (Expression)Visit(context.comparisonExpression());

            var leftExp = context.additiveExpression();

            if (leftExp != null)
            {
                var left = (Expression)Visit(leftExp);

                return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
            }
            return right;
        }

        public override Node VisitMultiplicativeExpression([NotNull] MuteGrammarParser.MultiplicativeExpressionContext context)
        {
            var right = (Expression)Visit(context.additiveExpression());

            var leftExp = context.multiplicativeExpression();

            if (leftExp != null)
            {
                var left = (Expression)Visit(leftExp);

                return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
            }
            return right;
        }

        public override Node VisitExpression([NotNull] MuteGrammarParser.ExpressionContext context)
        {
            var bin = context.memberAccessExpression();
            return Visit(bin);
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

        public override Node VisitMemberAccessExpression([NotNull] MuteGrammarParser.MemberAccessExpressionContext context)
        {
            var right = (Expression)Visit(context.ID());

            var leftExp = context.indexerExpression();

            if (leftExp != null)
            {
                var left = (Expression)Visit(leftExp);

                return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
            }
            return right;
            throw new NotImplementedException();
        }

        public override Node VisitPowExpression([NotNull] MuteGrammarParser.PowExpressionContext context)
        {
            var right = (Expression)Visit(context.multiplicativeExpression());

            var leftExp = context.powExpression();

            if (leftExp != null)
            {
                var left = (Expression)Visit(leftExp);

                return new BinaryExpression(context.GetPosition(), left.Type, left, right, context.@operator.GetTerminal());
            }
            return right;
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
