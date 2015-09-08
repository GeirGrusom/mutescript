using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;

namespace MuteScript
{

    public class Visitor : MuteGrammarBaseVisitor<string>
    {
        public override string VisitMethodClassMember([NotNull] MuteGrammarParser.MethodClassMemberContext context)
        {
            return base.VisitMethodClassMember(context);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
