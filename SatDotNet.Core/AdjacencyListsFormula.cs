using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatDotNet.Core
{
    class AdjacencyListsFormula : IBacktrackableFormula
    {
        Dictionary<IVariable, List<BacktrackableClause>> adjacencyLists;
        Dictionary<int, List<ILiteral>> assignedLiterals;
        int unsatisfiableLevel = -1;

        public AdjacencyListsFormula(ICnfFormula formula)
        {
            adjacencyLists = new();
            assignedLiterals = new();
            IsUnsatisfiable = false;

            Clauses = new HashSet<BacktrackableClause>();
            foreach (var clause in formula.Clauses)
            {
                var bclause = new BacktrackableClause(clause);
                Clauses.Add(bclause);
                foreach(var literal in clause.Literals)
                {
                    if(!adjacencyLists.TryGetValue(literal.Variable, out var adjClauses))
                    {
                        adjClauses = new();
                        adjacencyLists.Add(literal.Variable, adjClauses);
                    }
                    adjClauses.Add(bclause);
                }
                if (bclause.IsUnsatisfiable)
                    IsUnsatisfiable = true;
            }
        }

        public bool IsSafisfied => Clauses.All(c => c.IsSatisfied);
        public bool IsUnsatisfiable { get; private set; }
        public int UnitPropagationSteps { get; private set; }

        public ISet<BacktrackableClause> Clauses { get; }

        IEnumerable<IClause> ICnfFormula.Clauses => Clauses.Where(c=> !c.IsUnsatisfiable && !c.IsSatisfied);

        public void Assign(ILiteral literal, int decisionLevel)
        {
            if (!assignedLiterals.TryGetValue(decisionLevel, out var levelLiterals))
            {
                levelLiterals = new List<ILiteral>();
                assignedLiterals.Add(decisionLevel, levelLiterals);
            }
            levelLiterals.Add(literal);

            foreach (var clause in adjacencyLists[literal.Variable])
            {
                clause.Assign(literal, decisionLevel);
                if (clause.IsUnsatisfiable)
                {
                    IsUnsatisfiable = true;
                    unsatisfiableLevel = decisionLevel;
                }
            }
        }

        public void Backtrack(int decisionLevel)
        {
            List<int> laterDecisionLevels = assignedLiterals.Keys.Where(k => k > decisionLevel).ToList();
            foreach (int dl in laterDecisionLevels)
            {
                assignedLiterals.Remove(dl, out var levelLiterals);
                foreach (var literal in levelLiterals)
                    foreach (var clause in adjacencyLists[literal.Variable])
                        clause.Backtrack(decisionLevel);
            }

            if(unsatisfiableLevel > decisionLevel)
            {
                IsUnsatisfiable = false;
                unsatisfiableLevel = -1;
            }
        }

        public IEnumerable<ILiteral> GetAssignment()
        {
            foreach (var list in assignedLiterals.Values)
                foreach (var literal in list)
                    yield return literal;
        }

        public void UnitPropagate(int decisionLevel)
        {
            BacktrackableClause clause;
            while ((clause = GetUnitClause()) != null){
                var literal = clause.Literals.First();
                Assign(literal, decisionLevel);
                UnitPropagationSteps += 1;
            }
        }

        BacktrackableClause GetUnitClause()
        {
            foreach (var clause in Clauses)
                if (!clause.IsSatisfied && !clause.IsUnsatisfiable && clause.Literals.Count == 1)
                    return clause;
            return null;
        }
    }

    class BacktrackableClause : IClause
    {
        Dictionary<int, List<ILiteral>> unsatisfiedLiterals;
        int satisfactionLevel = -1;

        public BacktrackableClause(IClause clause)
        {
            Literals = new HashSet<ILiteral>(clause.Literals);
            unsatisfiedLiterals = new();
            IsSatisfied = false;
        }
        public ISet<ILiteral> Literals { get; }
        public void Assign(ILiteral literal, int decisionLevel)
        {
            if (Literals.Contains(literal))
            {
                // clause is satisfied by this literal
                if (!IsSatisfied)
                {
                    IsSatisfied = true;
                    satisfactionLevel = decisionLevel;
                    return;
                }
            }

            var oppositeLiteral = literal.Negate();
            if (Literals.Contains(oppositeLiteral))
            {
                // oppositeLiteral is False -> remove it
                Literals.Remove(oppositeLiteral);

                // remember that the literal was removed
                if (!unsatisfiedLiterals.TryGetValue(decisionLevel, out var levelLiterals))
                {
                    levelLiterals = new List<ILiteral>();
                    unsatisfiedLiterals.Add(decisionLevel, levelLiterals);
                }
                levelLiterals.Add(oppositeLiteral);
            }
            
        }

        public void Backtrack(int decisionLevel)
        {
            List<int> laterDecisionLevels = unsatisfiedLiterals.Keys.Where(k => k > decisionLevel).ToList();
            foreach(int dl in laterDecisionLevels)
            {
                unsatisfiedLiterals.Remove(dl, out var literals);
                Literals.UnionWith(literals);
            }

            if(decisionLevel < satisfactionLevel)
            {
                IsSatisfied = false;
            }
        }

        public bool IsSatisfied { get; private set; }
        public bool IsUnsatisfiable => !IsSatisfied && Literals.Count == 0;

        IEnumerable<ILiteral> IClause.Literals => Literals;
    }
}
