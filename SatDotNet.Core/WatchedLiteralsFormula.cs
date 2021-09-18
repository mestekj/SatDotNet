using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatDotNet.Core
{
    class WatchedLiteralsFormula:IBacktrackableFormula
    {
        Dictionary<ILiteral, Dictionary<WatchedLiteralsClause, int>>[] watchedLiterals;

        Dictionary<ILiteral, int> assignedLiteralsDecisionLevels;
        List<ILiteral> assignedLiterals;

        List<Tuple<WatchedLiteralsClause, int>> satisfiedClauses;
        HashSet<WatchedLiteralsClause> unitClauses;

        public WatchedLiteralsFormula(ICnfFormula formula)
        {
            assignedLiterals = new();
            assignedLiteralsDecisionLevels = new();
            watchedLiterals = new Dictionary<ILiteral, Dictionary<WatchedLiteralsClause, int>>[2];
            watchedLiterals[0] = new();
            watchedLiterals[1] = new();

            satisfiedClauses = new();
            unitClauses = new();

            Clauses = new();
            foreach(IClause clause in formula.Clauses)
            {
                var watchedLiteralsClause = new WatchedLiteralsClause(clause, assignedLiteralsDecisionLevels);
                Clauses.Add(watchedLiteralsClause);

                switch (watchedLiteralsClause.Literals.Count)
                {
                    case 0:
                        IsUnsatisfiable = true;
                        break;
                    case 1:
                        unitClauses.Add(watchedLiteralsClause);
                        break;
                    default:
                        AddWatchedLiteral(watchedLiterals[0], watchedLiteralsClause.Literals[0], watchedLiteralsClause, 0);
                        AddWatchedLiteral(watchedLiterals[1], watchedLiteralsClause.Literals[1], watchedLiteralsClause, 1);
                        break;
                }
            }
        }

        public bool IsSafisfied => satisfiedClauses.Count == Clauses.Count;

        public bool IsUnsatisfiable { 
            get; 
            private set; 
        } = false;

        public int UnitPropagationSteps { get; private set; } = 0;
        public int CheckedClauses { get; private set; } = 0;

        List<WatchedLiteralsClause> Clauses { get; set; }

        IEnumerable<IClause> ICnfFormula.Clauses
        {
            get
            {
                foreach (var clause in Clauses)
                    if (!clause.IsKnownToBeSatisfied)
                        yield return clause;
            }
        }

        public void Assign(ILiteral literal, int decisionLevel)
        {
            // add to assignedLiterals
            assignedLiterals.Add(literal);
            assignedLiteralsDecisionLevels.Add(literal, decisionLevel);

            // check and update WatchedLiterals
            UpdateWatchedLiterals(0, literal, decisionLevel);
            if(!IsUnsatisfiable)
                UpdateWatchedLiterals(1, literal, decisionLevel);
        }

        void UpdateWatchedLiterals(int watchedLiteralsIndex, ILiteral literal, int decisionLevel)
        {
            var thisWatchedLiterals = watchedLiterals[watchedLiteralsIndex];
            var otherWatchedLiterals = watchedLiterals[1 - watchedLiteralsIndex];

            if (thisWatchedLiterals.TryGetValue(literal, out var clausesWhereWatched))
            {
                // mark all as satisfied
                foreach (var clauseIndexPair in clausesWhereWatched)
                {
                    CheckedClauses++;
                    var clause = clauseIndexPair.Key;
                    MarkClauseAsSatisfied(clause, decisionLevel);
                }
            }

            literal = literal.Negate();
            if (thisWatchedLiterals.TryGetValue(literal, out clausesWhereWatched))
            {
                var clausesToUnwatch = new List<WatchedLiteralsClause>();
                foreach (var clauseIndexPair in clausesWhereWatched)
                {
                    CheckedClauses++;
                    var clause = clauseIndexPair.Key;
                    if (clause.IsKnownToBeSatisfied)
                        continue;

                    var watchedLiteralIndex = clauseIndexPair.Value;

                    var newWatchedLiteralIndexPair = GetLiteralToWatch(clause, watchedLiteralIndex, otherWatchedLiterals, out var noLiteralFound, out var isClauseSatisfied);

                    if (isClauseSatisfied)
                    {
                        MarkClauseAsSatisfied(clause, decisionLevel);
                    }
                    else if (noLiteralFound)
                    {
                        // looking for second literal to watch failed
                        unitClauses.Add(clause);
                    }
                    else
                    {
                        // switch watch to new literal
                        var newWatchedLiteral = newWatchedLiteralIndexPair.Item1;
                        var indexInClause = newWatchedLiteralIndexPair.Item2;

                        AddWatchedLiteral(thisWatchedLiterals, newWatchedLiteral, clause, indexInClause);
                        clausesToUnwatch.Add(clause);
                    }
                }
                foreach (var clause in clausesToUnwatch)
                    clausesWhereWatched.Remove(clause);
            }
        }

        private void MarkClauseAsSatisfied(WatchedLiteralsClause clause, int decisionLevel)
        {
            if (!clause.IsKnownToBeSatisfied)
            {
                clause.IsKnownToBeSatisfied = true;
                satisfiedClauses.Add(Tuple.Create(clause, decisionLevel));
            }
        }

        void AddWatchedLiteral(Dictionary<ILiteral, Dictionary<WatchedLiteralsClause, int>> watchedLiterals, ILiteral literal, WatchedLiteralsClause clause, int indexInClause)
        {
            if (!watchedLiterals.TryGetValue(literal, out var clausesWhereNewWatched))
            {
                clausesWhereNewWatched = new();
                watchedLiterals.Add(literal, clausesWhereNewWatched);
            }
            clausesWhereNewWatched.Add(clause, indexInClause);
        }

        Tuple<ILiteral, int> GetLiteralToWatch(WatchedLiteralsClause clause, int watchedLiteralIndex, Dictionary<ILiteral, Dictionary<WatchedLiteralsClause, int>> otherWatchedLiterals, out bool noLiteralFound, out bool isSatisfied)
        {
            noLiteralFound = false;
            isSatisfied = false;

            foreach (var literalIndexPair in clause.EnumerateLiteralsStartingAfter(watchedLiteralIndex))
            {
                var candidateLiteral = literalIndexPair.Item1;
                var candidateLiteralIndex = literalIndexPair.Item2;

                if (assignedLiteralsDecisionLevels.ContainsKey(candidateLiteral))
                {
                    isSatisfied = true;
                    return null;
                }

                if (!assignedLiteralsDecisionLevels.ContainsKey(candidateLiteral.Negate()))
                {
                    if(!IsLiteralWatchedInClause(otherWatchedLiterals, candidateLiteral, clause))
                        return literalIndexPair;
                }
            }
            // no suitable literal found
            noLiteralFound = true;
            return null;
        }

        private bool IsLiteralWatchedInClause(Dictionary<ILiteral, Dictionary<WatchedLiteralsClause, int>> watchedLiterals , ILiteral literal, WatchedLiteralsClause clause)
        {
            if (watchedLiterals.TryGetValue(literal, out var clausesWhereWatched))
                return clausesWhereWatched.ContainsKey(clause);
            else
                return false;
        }

        public void Backtrack(int decisionLevel)
        {
            // remove from assignedLiterals 
            int i;
            for (i = assignedLiterals.Count-1; i >= 0; i--)
            {
                var literal = assignedLiterals[i];
                if (assignedLiteralsDecisionLevels[literal] > decisionLevel)
                    assignedLiteralsDecisionLevels.Remove(literal);
                else break;
            }
            assignedLiterals.RemoveRange(i + 1, assignedLiterals.Count - (i+1));

            // restore satisfied clauses
            for (i = satisfiedClauses.Count - 1; i >= 0; i--)
            {
                CheckedClauses++;
                var clauseLevelPair = satisfiedClauses[i];
                if (clauseLevelPair.Item2 > decisionLevel)
                    clauseLevelPair.Item1.IsKnownToBeSatisfied = false;
                else break;
            }
            satisfiedClauses.RemoveRange(i + 1, satisfiedClauses.Count - (i + 1));

            IsUnsatisfiable = false;
        }

        public IEnumerable<ILiteral> GetAssignment()
        {
            return assignedLiterals;
        }

        public void UnitPropagate(int decisionLevel)
        {
            while(unitClauses.Count > 0)
            {
                if (IsUnsatisfiable)
                {
                    // solver will backtrack => no reason to perform unitProp
                    unitClauses.Clear();
                    return;
                }
                    
                var clause = unitClauses.First();
                unitClauses.Remove(clause);
                if (clause.IsKnownToBeSatisfied)
                    continue;
                else if (clause.CheckIsSatisfied())
                {
                    MarkClauseAsSatisfied(clause, decisionLevel);
                    continue;
                }

                var literal = ((IClause)clause).Literals.FirstOrDefault();
                if (literal == null)
                    // this clause is unsat
                    IsUnsatisfiable = true;
                else
                {
                    UnitPropagationSteps++;
                    Assign(literal, decisionLevel);
                    MarkClauseAsSatisfied(clause, decisionLevel);
                }
            }
        }
    }

    class WatchedLiteralsClause : IClause
    {
        public List<ILiteral> Literals { get; }
        Dictionary<ILiteral, int> assignedLiteralsDecisionLevels; // to check if literal was decided

        IEnumerable<ILiteral> IClause.Literals
        {
            get
            {
                foreach (var literal in Literals)
                    if (!assignedLiteralsDecisionLevels.ContainsKey(literal.Negate()) && !assignedLiteralsDecisionLevels.ContainsKey(literal))
                        yield return literal;
            }
        }


        public bool IsKnownToBeSatisfied
        {
            get; set;
        } = false;

        public bool CheckIsSatisfied()
        {
            foreach(var literal in Literals)
            {
                if (assignedLiteralsDecisionLevels.ContainsKey(literal))
                    return true;
            }
            return false;
        }

        public WatchedLiteralsClause(IClause clause, Dictionary<ILiteral, int> assignedLiteralsDecisionLevels)
        {
            Literals = new(clause.Literals);
            this.assignedLiteralsDecisionLevels = assignedLiteralsDecisionLevels;
        }

        public IEnumerable<Tuple<ILiteral, int>> EnumerateLiteralsStartingAfter(int index)
        {
            for (int i = index + 1; i < Literals.Count; i++)
            {
                yield return Tuple.Create(Literals[i], i);
            }
            for (int i = 0; i <= index; i++)
            {
                yield return Tuple.Create(Literals[i], i);
            }
        }
    }
}
