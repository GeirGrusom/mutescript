using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MuteScript.EqualityMethods;

namespace MuteScript.ParseTree
{
    public sealed class SourcePositionInfo
    {
        public static readonly SourcePositionInfo Empty = new SourcePositionInfo("", 0, 0, 0);
        public string Filename { get; }
        public int Line { get; }
        public int Character { get; }
        public int Length { get; }
        public SourcePositionInfo(string filename, int line, int character, int length)
        {
            Filename = filename;
            Line = line;
            Character = character;
            Length = length;
        }

        public override string ToString()
        {
            return $"{Line}:{Character}";
        }
    }


    public abstract class Node
    {
        public SourcePositionInfo Source { get; }

        protected Node(SourcePositionInfo pos)
        {
            Source = pos;
        }

        public virtual void ToString(int tab, StringBuilder builder)
        {
            builder.Append('\t', tab);
            builder.Append(ToString());
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(0, sb);
            return sb.ToString();
        }
    }

    public class Module : Node
    {
        public string Name { get; }

        public ImmutableArray<Node> Imports { get; }

        public ImmutableArray<Node> Members { get; }


        public override void ToString(int tab, StringBuilder builder)
        {
            builder.Append('\t', tab);
            builder.AppendLine($"module {Name}");
            builder.AppendLine();
            foreach(var mem in Members)
            {
                mem.ToString(tab, builder);
            }
        }

        public Module(SourcePositionInfo pos, string name, IEnumerable<Node> imports, IEnumerable<Node> members)
            : base(pos)
        {
            Name = name;
            Imports = imports.ToImmutableArray();
            Members = members.ToImmutableArray();
        }
    }

    public class SymbolExpression : Expression
    {
        public Terminal Symbol { get; }

        public SymbolExpression(SourcePositionInfo pos, Terminal symbol)
            : base(pos, null)
        {
            Symbol = symbol;
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }

    public class Import : Node
    {
        public Node Module { get; }

        public override string ToString()
        {
            return $"import {Module}";
        }

        public Import(SourcePositionInfo source, Terminal moduleName)
            : base(source)
        {
            Module = new NamedModule(source, moduleName);
        }

        public Import(SourcePositionInfo source, Module module)
            : base(source)
        {
            Module = module;
        }
    }

    public class NamedModule : Node
    {
        public Terminal Module { get; }

        public NamedModule(SourcePositionInfo source, Terminal name)
            : base(source)
        {
            Module = name;
        }

        public override string ToString()
        {
            return Module.ToString();
        }
    }

    public class Class : Node
    {
        public Terminal Access { get; }

        public Terminal StorageClass { get; }

        public Terminal Name { get; }

        public Tuple DefaultConstructor { get; }

        public ImmutableArray<Node> Members { get; }

        public ImmutableArray<Node> GenericArguments { get; }

        public override void ToString(int tab, StringBuilder builder)
        {
            builder.Append('\t', tab);
            if(Access != null)
            {
                builder.Append(Access);
                builder.Append(" ");
            }
            if(StorageClass != null)
            {
                builder.Append(StorageClass);
                builder.Append(" ");
            }
            builder.Append($"class {Name}");
            if(GenericArguments.Any())
            {
                builder.Append($"<{string.Join(", ", GenericArguments)}>");
            }
            if(DefaultConstructor !=null)
            {
                DefaultConstructor.ToString(0, builder);
            }
            builder.AppendLine();
            builder.Append('\t', tab);
            builder.AppendLine("{");
            foreach(var item in Members)
            {
                item.ToString(tab + 1, builder);
            }
            builder.AppendLine();
            builder.Append('\t', tab);
            builder.AppendLine("}");
        }

        public Class(SourcePositionInfo pos, Terminal access, Terminal storageClass, Terminal name, Tuple defaultConstructor, IEnumerable<Node> members, IEnumerable<Node> genericArguments)
            : base(pos)
        {
            Access = access;
            StorageClass = storageClass;
            Name = name;
            DefaultConstructor = defaultConstructor;
            Members = members.ToImmutableArray();
            GenericArguments = genericArguments.ToImmutableArray();
        }
    }

    public class TypeGenericArgument : Node
    {
        public Terminal Name { get; }

        public TypeGenericArgument(SourcePositionInfo pos, Terminal name)
            : base(pos)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class ConstantGenericArgument : Node
    {
        public Node Type { get; }
        public Terminal Name { get; }

        public ConstantGenericArgument(SourcePositionInfo pos, Node type, Terminal name)
            : base(pos)
        {
            Type = type;
            Name = name;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class Method : Node
    {
        public Terminal Access { get; }

        public Terminal Pure { get; }

        public Terminal Name { get; }

        public ImmutableArray<Node> GenericArguments { get; }

        public Tuple Parameters { get; }

        public Node Body { get; }

        public Method(SourcePositionInfo pos, Terminal access, Terminal pure, Terminal name, IEnumerable<Node> genericArguments, Tuple parameters, Node body)
            : base(pos)
        {
            Access = access;
            Pure = pure;
            Name = name;
            GenericArguments = genericArguments.ToImmutableArray();
            Parameters = parameters;
            Body = body;
        }

        public override void ToString(int tab, StringBuilder builder)
        {
            builder.Append('\t', tab);
            builder.Append(Access);
            builder.Append(" ");
            if(Body == null)
            {
                builder.Append("defer ");
            }
            builder.Append(Name);

            if(GenericArguments.Any())
            {
                builder.Append($"<{string.Join(", ", GenericArguments)}>");
            }

            builder.Append(Parameters);

            if (Body == null)
                return;

            if(Body is StatementBlock)
            {
                builder.AppendLine();
                Body.ToString(tab + 1, builder);
            }
            else
            {
                builder.Append($" => {Body}");
            }

        }
    }

    public sealed class Terminal : Node, IEquatable<Terminal>, IEquatable<string>
    {
        public string Value { get; }

        public Terminal(SourcePositionInfo pos, string value)
            : base(pos)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var terminal = obj as Terminal;
            if (terminal != null)
                return Equals(terminal);
            var str = obj as string;
            if (str != null)
                return Equals(str);
            return false;
        }

        public bool Equals(Terminal other)
        {
            return Value == other.Value;
        }

        public bool Equals(string other)
        {
            return Value == other;
        }
    }

    public class IndexerExpression : Expression
    {
        public Expression Value { get; }
        public Expression Index { get; }

        public IndexerExpression(SourcePositionInfo pos, Expression value, Expression index)
            : base(pos, null)
        {
            Value = value;
            Index = index;
        }
        public override string ToString()
        {
            if(Index is ConstIntegerExpression)
            {
                return $"{Value}.{Index}";
            }
            else
            {
                return $"{Value}[{Index}]";
            }
        }
    }

    public class ConstIntegerExpression : Expression
    {
        public Terminal Value { get; }

        public ConstIntegerExpression(SourcePositionInfo pos, Terminal value)
            : base(pos, new TypeReference(value.Source, null, new Terminal(value.Source, "int"), Enumerable.Empty<Node>()))
        {
            Value = value;
        }
        public override string ToString() => Value.ToString();
    }

    public class ConstStringExpression : Expression
    {
        public Terminal Value { get; }

        public ConstStringExpression(SourcePositionInfo pos, Terminal value)
            : base(pos, new TypeReference(value.Source, null, new Terminal(value.Source, "int"), Enumerable.Empty<Node>()))
        {
            Value = value;
        }
        public override string ToString() => Value.ToString();
    }

    public class MemberExpression : Expression
    {
        public Node Member { get; }

        public MemberExpression(SourcePositionInfo pos, Node type, Node member)
            : base(pos, type)
        {
            Member = member;
        }

        public override string ToString()
        {
            return Member.ToString();
        }
    }

    public class StatementBlock : Expression
    {
        public ImmutableList<Expression> Statements { get; }

        public StatementBlock(SourcePositionInfo pos, Node type, IEnumerable<Expression> statements)
            : base(pos, type)
        {
            Statements = statements.ToImmutableList();
        }

        public override string ToString()
        {
            return $"{{ {string.Join(Environment.NewLine, Statements)} }}";
        }

        public override void ToString(int tab, StringBuilder builder)
        {
            builder.Append('\t', tab - 1);
            builder.AppendLine("{");
            foreach(var item in Statements)
            {
                item.ToString(tab, builder);
            }
            builder.AppendLine();
            builder.Append('\t', tab - 1);
            builder.AppendLine("}");
        }
    }

    public class ConstantExpression : Expression
    {
        public Terminal Value { get; }

        public ConstantExpression(SourcePositionInfo pos, Node type, Terminal value)
            : base(pos, type)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; }
        public Expression IsTrue { get; }
        public Expression IsFalse { get; }

        public ConditionalExpression(SourcePositionInfo pos, Node type, Expression condition, Expression isTrue, Expression isFalse)
            : base(pos, type)
        {
            Condition = condition;
            IsTrue = isTrue;
            IsFalse = isFalse;
        }

        public override string ToString()
        {
            return $"if {Condition} {IsTrue} else {IsFalse}";
        }
    }

    public class UnaryExpression : Expression
    {
        public Expression Operand { get; }

        public Terminal Operator { get; }

        public UnaryExpression(SourcePositionInfo pos, Expression operand, Terminal @operator)
            : base(pos, operand.Type)
        {
            Operand = operand;
            Operator = @operator;
        }

        public override string ToString()
        {
            return $"{Operator}{Operand}";
        }
    }
    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public Terminal Operator { get; }

        public BinaryExpression(SourcePositionInfo pos, Node type, Expression left, Expression right, Terminal @operator)
            : base(pos, type)
        {
            Left = left;
            Right = right;
            Operator = @operator;
        }

        public override string ToString() => $"{Left} {Operator} {Right}";
    }

    public class TupleExpression : Expression
    {
        public ImmutableArray<Expression> Arguments { get; }

        public TupleExpression(SourcePositionInfo pos, Node type, IEnumerable<Expression> arguments)
            : base(pos, type)
        {
            Arguments = arguments.ToImmutableArray();
        }

        public override string ToString() => $"({string.Join(", ", Arguments)})";
    }

    public class Field : Node
    {
        public Terminal StorageClass { get; }

        public Terminal Access { get; }

        public Terminal Name { get; }

        public Node Type { get; }

        public Expression Expression { get; }

        public Field(SourcePositionInfo pos, Terminal access, Terminal storageClass, Terminal name, Node dataType, Expression initializationExpression)
            : base(pos)
        {
            Access = access;
            StorageClass = storageClass;
            Name = name;
            Type = dataType;
            Expression = initializationExpression;
        }

        public override string ToString()
        {
            return $"{Access} {Name}" + (Type != null ? " : " + Type.ToString() : "") + (Expression != null ? " <- " + Expression.ToString() : "");
        }
    }

    public class Tuple : Node
    {

        public ImmutableArray<Node> Members { get; }

        public Tuple(SourcePositionInfo pos, IEnumerable<Node> members)
            : base(pos)
        {
            Members = members.ToImmutableArray();
        }

        public override string ToString()
        {
            return $"({string.Join(", ", Members)})";
        }
    }

    public class TupleMember : Node
    {
        public Terminal StorageClass { get; }
        public Terminal Access { get; }
        public Terminal Name { get; }
        public Node Type { get; }
        public Expression Expression { get; }

        public TupleMember(SourcePositionInfo pos, Terminal access, Terminal storageClass, Terminal name, Node type, Expression expression)
            : base(pos)
        {
            Access = access;
            StorageClass = storageClass;
            Name = name;
            Type = type;
            Expression = expression;
        }

        public override void ToString(int tab, StringBuilder builder)
        {
            if (Access != null)
            {
                builder.Append(Access);
                builder.Append(" ");
            }
            builder.Append($"{StorageClass} {Name}");
            if(Type != null)
            {
                builder.Append($" : {Type}");
            }
            if(Expression != null)
            {
                builder.Append($" <- {Expression}");
            }
        }
    }

    public class DataType : Node
    {
        public Node Type { get; }
        public Terminal Nullable { get; }
        public DataType(SourcePositionInfo pos, Node type, Terminal nullable)
            : base(pos)
        {
            Type = type;
            Nullable = nullable;
        }

        public override string ToString()
        {
            if (Equals(Nullable, "?"))
                return "{Type}?";
            return Type.ToString();
        }
    }

    public class RangeType : Node
    {
        public DataType ElementType { get; }

        public RangeType(SourcePositionInfo pos, DataType elementType)
            : base(pos)
        {
            ElementType = elementType;
        }

        public override string ToString()
        {
            return $"[{ElementType}]";
        }
    }

    public abstract class Expression : Node
    {
        public Node Type { get; }

        protected Expression(SourcePositionInfo pos, Node type)
            : base(pos)
        {
            Type = type;
        }
    }

    public class TypeReference : Node
    {
        public static readonly TypeReference Boolean = new TypeReference(SourcePositionInfo.Empty, null, new Terminal(SourcePositionInfo.Empty, "bool"), Enumerable.Empty<Node>());
        public static readonly TypeReference Int = new TypeReference(SourcePositionInfo.Empty, null, new Terminal(SourcePositionInfo.Empty, "int"), Enumerable.Empty<Node>());
        public static readonly TypeReference Float = new TypeReference(SourcePositionInfo.Empty, null, new Terminal(SourcePositionInfo.Empty, "float"), Enumerable.Empty<Node>());
        public static readonly TypeReference String = new TypeReference(SourcePositionInfo.Empty, null, new Terminal(SourcePositionInfo.Empty, "string"), Enumerable.Empty<Node>());
        public static readonly TypeReference Void = new TypeReference(SourcePositionInfo.Empty, null, new Terminal(SourcePositionInfo.Empty, "void"), Enumerable.Empty<Node>());
        public static readonly TypeReference Never = new TypeReference(SourcePositionInfo.Empty, null, new Terminal(SourcePositionInfo.Empty, "never"), Enumerable.Empty<Node>());
        public static readonly TypeReference Null = new TypeReference(SourcePositionInfo.Empty, null, new Terminal(SourcePositionInfo.Empty, "null"), Enumerable.Empty<Node>());

        public Terminal Module { get; }

        
        public Terminal Name { [return:NotNull] get; }
        public ImmutableArray<Node> GenericArguments { [return:NotNull] get; }

        public TypeReference(SourcePositionInfo pos, Terminal module, Terminal name, IEnumerable<Node> genericArguments)
            : base(pos)
        {
            Module = module;
            Name = name;
            GenericArguments = genericArguments.ToImmutableArray();
        }

        public override string ToString()
        {
            string genericArguments;
            if (GenericArguments.Any())
                genericArguments = "<" + string.Join(", ", GenericArguments) + ">";
            else
                genericArguments = "";

            if (ReferenceEquals(Module, null))
                return $"{Module}.{Name}{GenericArguments}";
            return $"{Name}{GenericArguments}";
        }
    }    

    public abstract class Visitor
    {
        public virtual Node Visit(Node node)
        {
            return node;
        }

        public virtual Node Visit(Module module)
        {
            return module;
        }

        public virtual Node Visit(Class @class)
        {
            return @class;
        }
    }
}
