using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MuteScript;
using MuteScript.ParseTree;

namespace UnitTests
{
    [TestFixture]
    public class ParserTests
    {

        private static Module ParseModule(string code)
        {
            var parser = new MuteScriptParser();
            return  (Module)parser.Parse(code);
        }

        private static Class ParseClass(string code)
        {
            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream(code));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));

            return (Class)parser.VisitTypeDefinitionStatement(muteParser.typeDefinitionStatement());
        }

        private static readonly SourcePositionInfo EmptySourcePosition = new SourcePositionInfo("", 0, 0, 0);
        
        private static Expression ParseExpression(string code)
        {
            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream(code));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));
            var exp = muteParser.expression();
            return (Expression)parser.VisitExpression(exp);
        }

        private static Field ParseField(string code)
        {
            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream(code));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));
            return (Field)parser.VisitClassMember(muteParser.classMember());
        }

        private static Method ParseMethod(string code)
        {
            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream(code));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));

            return (Method)parser.VisitClassMember(muteParser.classMember());

        }

        [Test]
        public void Module_NamedFoo_ReturnsModule()
        {
            var module = ParseModule("module Foo;");
            Assert.That(module, Is.Not.Null);
            Assert.That(module.Name, Is.EqualTo("Foo"));
        }

        [Test]
        public void Class_NamedBar_ReturnsClassNamedBar()
        {
            var @class = ParseClass("class Bar { }");

            Assert.That(@class.Name.Value, Is.EqualTo("Bar"));
        }

        [TestCase("immutable")]
        [TestCase("mutable")]
        [TestCase(null)]
        public void ImmutableClass_NamedFoo_ReturnsModuleWithClassFoo_Immutable(string keyword)
        {
            var item= ParseClass($"{keyword} class Bar {{ }}");

            Assert.That(item.StorageClass?.Value, Is.EqualTo(keyword));
        }

        [TestCase("public")]
        [TestCase("private")]
        [TestCase(null)]
        public void ClassWithAccess_ReturnsClassWithExpectedAccess(string keyword)
        {
            var item = ParseClass($"{keyword} class Bar {{ }}");

            Assert.That(item.Access?.ToString(), Is.EqualTo(keyword));
        }

        [TestCase("mutable")]
        [TestCase("const")]
        [TestCase("immutable")]
        [TestCase(null)]
        public void Field_WithStorageClass(string storageClass)
        {
            var item = ParseField($"{storageClass} baz : int");

            Assert.That(item.StorageClass?.Value, Is.EqualTo(storageClass));
        }

        [Test]
        public void Method_NameIsFoo()
        {
            var item = ParseMethod("foo() : int { }");

            Assert.That(item.Name?.ToString(), Is.EqualTo("foo"));
        }

        [TestCase("!", ".")]
        [TestCase("^", ".")]
        [TestCase("^", "*")]
        [TestCase("*", "+")]
        [TestCase(">", "+")]
        [TestCase("=", ">")]
        public void Operator_HasHigherPresedenceThan_Operator(string opHigh, string opLow)
        {
            var item = (BinaryExpression)ParseExpression($"abc {opLow} def {opHigh} ghi");

            Console.WriteLine(item.ToString());

            Assert.That(item.Operator.Value, Is.EqualTo(opHigh));
            Assert.That(((BinaryExpression)item.Left).Operator.Value, Is.EqualTo(opLow));
        }

        [TestCase("!", ".")]
        [TestCase("^", ".")]
        [TestCase("^", "*")]
        [TestCase("*", "+")]
        [TestCase(">", "+")]
        [TestCase("=", ">")]
        public void Operator_HasHigherPresedenceThan_Operator_Reverse(string opHigh, string opLow)
        {
            var item = (BinaryExpression)ParseExpression($"abc {opHigh} def {opLow} ghi");

            Console.WriteLine(item.ToString());

            Assert.That(item.Operator.Value, Is.EqualTo(opHigh));
            Assert.That(((BinaryExpression)item.Right).Operator.Value, Is.EqualTo(opLow));
        }

        [TestCase(">")]
        [TestCase("+")]
        [TestCase("-")]
        [TestCase("*")]
        [TestCase(".")]
        public void Operator_Assignment_Operator(string op)
        {
            var item = (BinaryExpression)ParseExpression($"abc <- def {op} ghi");

            Console.WriteLine(item.ToString());

            Assert.That(item.Operator.Value, Is.EqualTo("<-"));
            Assert.That(((BinaryExpression)item.Right).Operator.Value, Is.EqualTo(op));
        }

        [TestCase("+")]
        [TestCase("-")]
        [TestCase("*")]
        [TestCase("/")]
        [TestCase("^")]
        [TestCase("%")]
        [TestCase(".")]
        [TestCase("!")]
        [TestCase("<-")]
        [TestCase(">")]
        [TestCase("<")]
        [TestCase(">=")]
        [TestCase("<=")]
        [TestCase("=")]
        [TestCase("<>")]
        public void Operator_ReturnsCorrectType(string op)
        {
            var item = (BinaryExpression)ParseExpression($"def {op} abc");


            Assert.That(item.Operator.Value, Is.EqualTo(op));
        }

        [Test]
        public void ConstInteger_ReturnsConstIntegerExpression()
        {
            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream("123"));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));

            var result = (ConstIntegerExpression)parser.VisitConstExpression(muteParser.constExpression());
            Assert.That(result?.ToString(), Is.EqualTo("123"));

        }

        [Test]
        public void TestFoo()
        {
            const string input = @"
module foo

public class bar
{
    public meth(const arg : int) : bool => abc <- 100
}
";

            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream(input));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));

            var cmp = parser.VisitCompileUnit(muteParser.compileUnit());
            Console.WriteLine(cmp.ToString());
        }

        [TestCase("123", typeof(ConstIntegerExpression))]
        public void ConstExpression_ReturnsTerminal(string input, Type expectedType)
        {
            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream(input));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));

            var result = parser.VisitConstExpression(muteParser.constExpression());

            Assert.That(result, Is.TypeOf(expectedType));
            Assert.That(result?.ToString(), Is.EqualTo(input));
            
        }

        [Test]
        public void Foo()
        {
            var parser = new MuteScriptParser();
            var lexer = new MuteGrammarLexer(new Antlr4.Runtime.AntlrInputStream("{ 123 . 456 }"));
            var muteParser = new MuteGrammarParser(new Antlr4.Runtime.UnbufferedTokenStream(lexer));

            var result = parser.VisitStatementBlock(muteParser.statementBlock());
        }
    }
}
