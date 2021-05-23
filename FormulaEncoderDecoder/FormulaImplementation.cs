using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;

namespace SatDotNet.FormulaEncoderDecoder
{
    class Variable : IVariable
    {
        public Variable()
        {
            NegativeLiteral = new Literal(this, false);
            PositiveLiteral = new Literal(this, true);
        }

        public bool Equals(IVariable other)
        {
            return object.ReferenceEquals(this, other);
        }

        public ILiteral NegativeLiteral { get; }

        public ILiteral PositiveLiteral { get; }
    }

    class Literal : ILiteral
    {
        public Literal(IVariable variable, bool isPositive)
        {
            Variable = variable ?? throw new ArgumentNullException(nameof(variable));
            IsPositive = isPositive;
        }

        public IVariable Variable { get; private set; }

        public bool IsPositive { get; private set; }

        public bool Equals(ILiteral other)
        {
            return this.Variable.Equals(other.Variable) && this.IsPositive == other.IsPositive;
        }

        public ILiteral Negate()
        {
            return IsPositive ? Variable.NegativeLiteral : Variable.PositiveLiteral;
        }
    }

    class Clause : IClause
    {
        public Clause(IEnumerable<ILiteral> literals)
        {
            Literals = literals ?? throw new ArgumentNullException(nameof(literals));
        }

        public IEnumerable<ILiteral> Literals { get; }
    }

    class CnfFormula : ICnfFormula
    {
        public CnfFormula(IEnumerable<IClause> clauses)
        {
            Clauses = clauses ?? throw new ArgumentNullException(nameof(clauses));
        }

        public IEnumerable<IClause> Clauses { get; }
    }
}
