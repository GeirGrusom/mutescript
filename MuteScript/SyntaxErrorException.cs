using MuteScript.ParseTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuteScript
{
    public class SyntaxErrorException : Exception
    {
        public Node Node { get; }
        public SyntaxErrorException(string message, Node node)
            : base(message)
        {
            Node = node;
        }

        public SyntaxErrorException(Node node)
            : this($"Syntax error in \"{node.Source.Filename}\" at line {node.Source.Line} character {node.Source.Character}: {node}", node)
        {

        }
    }
}
