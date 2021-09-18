using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;

namespace SatDotNet.Core
{
    public class Solver
    {
        public IDecisionHeuristic DecisionHeuristic { get; }

        public Solver(IDecisionHeuristic decisionHeuristic = null)
        {
            this.DecisionHeuristic = decisionHeuristic ?? new TakeFirstDecisionHeuristic();
        }

        /**
         * Solve formula.
         */
        public IPartialyAssignedFormula Solve(ICnfFormula formula, bool useWatchedLiterals=false)
        {
            IBacktrackableFormula bformula = useWatchedLiterals ? new WatchedLiteralsFormula(formula) : new AdjacencyListsFormula(formula);
            Solve(bformula, 0);
            return bformula;
        }

        private protected void Solve(IBacktrackableFormula f, int decisionLevel)
        {
            f.UnitPropagate(decisionLevel);
            if (f.IsSafisfied)
                return;
            if (f.IsUnsatisfiable)
                return;

            ILiteral literal = DecisionHeuristic.Suggest(f);

            f.Assign(literal, decisionLevel+1);
            Solve(f, decisionLevel + 1);
            if (!f.IsUnsatisfiable)
                return;

            f.Backtrack(decisionLevel);
            f.Assign(literal.Negate(), decisionLevel + 1);
            Solve(f, decisionLevel + 1);
            if (!f.IsUnsatisfiable)
                return;

            //f.IsUnsatisfiable = true;
            return;
        }

    }


    
}
