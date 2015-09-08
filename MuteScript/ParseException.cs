using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuteScript
{
    using ParseTree;
    public class ParseException : Exception
    {
        public SourcePositionInfo Position { get; }

        public ParseException(string message, SourcePositionInfo position)
            : base(message)
        {
            Position = position;
        }

        public ParseException(SourcePositionInfo position)
            : this($"Parsing failed in {position.Filename} at line {position.Line} character {position.Character}", position)
        {
            
        }
    }
}
