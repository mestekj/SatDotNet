using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatDotNet.Core
{

    public interface IDecisionHeuristic
    {
        ILiteral Suggest(IBacktrackableFormula f);
        int DecisionsCount { get; }
    }

    public interface IBacktrackableFormula : IPartialyAssignedFormula
    {
        

        void Assign(ILiteral literal, int decisionLevel);
        void Backtrack(int decisionLevel);
        
        void UnitPropagate(int decisionLevel);
        
        
    }

    public interface IPartialyAssignedFormula : ICnfFormula
    {
        bool IsSafisfied { get; }
        bool IsUnsatisfiable { get; }
        int UnitPropagationSteps { get; }
        IEnumerable<ILiteral> GetAssignment();
    }

    interface IUnitPropagator
    {
        void Propagate(IBacktrackableFormula f);
        int Steps { get; }
    }
}
