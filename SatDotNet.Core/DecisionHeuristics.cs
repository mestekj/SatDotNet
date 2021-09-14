using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatDotNet.Core
{
    class TakeFirstDecisionHeuristic : IDecisionHeuristic
    {
        public int DecisionsCount { get; private set; } = 0;

        public ILiteral Suggest(IBacktrackableFormula f)
        {
            DecisionsCount += 1;
            foreach(var clause in f.Clauses)
            {
                var literal = clause.Literals.FirstOrDefault();
                if (literal is not default(ILiteral))
                    return literal;
            }
            return null;
        }
    }
}
