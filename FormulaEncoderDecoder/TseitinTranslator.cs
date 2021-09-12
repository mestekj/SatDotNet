using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SatDotNet.Interfaces;

namespace SatDotNet.FormulaEncoderDecoder
{
    public class TseitinTranslator : IVisitor<List<Clause>>
    {
        Dictionary<NnfNode, Variable> TseitinVariables { get; }
        bool UseEquivalence { get; }
        NnfParser nnfParser { get; }

        public TseitinTranslator(bool useEquivalence)
        {
            UseEquivalence = useEquivalence;
            TseitinVariables = new Dictionary<NnfNode, Variable>();
            nnfParser = new NnfParser();
        }

        public IEnumerable<IVariable> GetAuxiliaryVariables()
        {
            return TseitinVariables.Values;
        }

        public IEnumerable<IVariable> GetOriginalVariables()
        {
            return nnfParser.Variables.Values;
        }

        public ICnfFormula TranslateFormula(string nnfFormula, out IVariable rootVariable)
        {
            var nnfTree = nnfParser.ParseFormula(nnfFormula);
            return TranslateFormula(nnfTree, out rootVariable);
        }

        CnfFormula TranslateFormula(NnfTree nnfTree, out IVariable rootVariable)
        {
            List<Clause> clauses = new List<Clause>();
            foreach(var node in nnfTree.Nodes)
            {
                var clausesForNode = GetClausesForNode(node);
                if (clausesForNode != null)
                    clauses.AddRange(clausesForNode);
            }
            // add unit clause for root
            rootVariable = GetVariableForNode(nnfTree.Root);
            var unitClause = new Clause(new List<ILiteral>() { rootVariable.PositiveLiteral });
            clauses.Add(unitClause);

            return new CnfFormula(clauses);
        }

        private protected IVariable GetVariableForNode(NnfNode node)
        {
            if (node is VariableNnfNode variableNode)
                return variableNode.Variable;

            if (!TseitinVariables.ContainsKey(node))
            {
                
                Variable variable = new Variable();
                TseitinVariables.Add(node, variable);
            }
            return TseitinVariables[node];
        }

        List<Clause> GetClausesForNode(NnfNode node)
        {
            return node.Accept(this);
        }

        List<Clause> IVisitor<List<Clause>>.VisitNode(AndNnfNode node)
        {
            var tseitinVariable = GetVariableForNode(node);
            var leftVariable = GetVariableForNode(node.Left);
            var rightVariable = GetVariableForNode(node.Right);

            /**
             * t => l & r    ...   !t || (l & r)    ...   (!t || l) & (!t || r)
             * l & r => t    ...   (!l || !r) || t  
             */

            List<Clause> clauses = new List<Clause>();

            if (UseEquivalence)
            {
                clauses.Add(new Clause(new List<ILiteral> { leftVariable.NegativeLiteral, rightVariable.NegativeLiteral, tseitinVariable.NegativeLiteral, }));

            }

            clauses.Add(new Clause(new List<ILiteral> { tseitinVariable.NegativeLiteral, leftVariable.PositiveLiteral }));
            clauses.Add(new Clause(new List<ILiteral> { tseitinVariable.NegativeLiteral, rightVariable.PositiveLiteral }));

            return clauses;
        }

        List<Clause> IVisitor<List<Clause>>.VisitNode(OrNnfNode node)
        {
            var tseitinVariable = GetVariableForNode(node);
            var leftVariable = GetVariableForNode(node.Left);
            var rightVariable = GetVariableForNode(node.Right);

            /**
             * t => l || r    ...   !t || l || r
             * l || r => t    ...   (!l & !r) || t   ...   (!l || t) & (!r || t)
             */

            List<Clause> clauses = new List<Clause>();

            if (UseEquivalence)
            {
                clauses.Add(new Clause(new List<ILiteral> { leftVariable.NegativeLiteral, tseitinVariable.PositiveLiteral }));
                clauses.Add(new Clause(new List<ILiteral> { rightVariable.NegativeLiteral, tseitinVariable.PositiveLiteral }));
            }

            clauses.Add(new Clause(new List<ILiteral> { tseitinVariable.NegativeLiteral, leftVariable.PositiveLiteral, rightVariable.PositiveLiteral }));

            return clauses;
        }

        List<Clause> IVisitor<List<Clause>>.VisitNode(NegationNnfNode node)
        {
            var tseitinVariable = GetVariableForNode(node);
            var negatedVariable = GetVariableForNode(node.Child);

            List<Clause> clauses = new List<Clause>();

            if (UseEquivalence)
                clauses.Add(new Clause(new List<ILiteral> { tseitinVariable.PositiveLiteral, negatedVariable.PositiveLiteral }));

            clauses.Add(new Clause(new List<ILiteral> { tseitinVariable.NegativeLiteral, negatedVariable.NegativeLiteral }));

            return clauses;
        }

        List<Clause> IVisitor<List<Clause>>.VisitNode(VariableNnfNode node)
        {
            return null;
        }
    }
}
