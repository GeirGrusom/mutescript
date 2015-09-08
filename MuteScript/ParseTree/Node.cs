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
    }

    public class Module : Node
    {
        public string Name { get; }

        public ImmutableArray<Node> Imports { get; }

        public ImmutableArray<Node> Members { get; }

        public override string ToString()
        {
            return $"module {Name}";
        }

        public Module(SourcePositionInfo pos, string name, IEnumerable<Node> imports, IEnumerable<Node> members)
            : base(pos)
        {
            Name = name;
            Imports = imports.ToImmutableArray();
            Members = members.ToImmutableArray();
        }
    }

    public class Import : Node
    {
        public Node Module { get; }

        public override string ToString()
        {
            return $"import {Module}";
        }

        public Import(SourcePositionInfo source, string moduleName)
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
        public string Module { get; }

        public NamedModule(SourcePositionInfo source, string name)
            : base(source)
        {
            Module = name;
        }

        public override string ToString()
        {
            return Module;
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

        public override string ToString()
        {
            return $"{Access} {StorageClass} {Name}({DefaultConstructor})\r\n{{ {string.Join("\r\n", Members)} }} ";
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
        public override string ToString() => $"{Value}[{Index}]";
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
