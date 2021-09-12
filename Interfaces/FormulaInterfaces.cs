using System;
using System.Collections.Generic;

namespace SatDotNet.Interfaces
{
    public interface IVariable : IEquatable<IVariable>
    {
        ILiteral PositiveLiteral { get; }
        ILiteral NegativeLiteral { get; }
        String Name { get; }
    }

    public interface ILiteral : IEquatable<ILiteral>
    {
        IVariable Variable { get; }
        bool IsPositive { get; }
        ILiteral Negate();
    }

    public interface IClause
    {
        IEnumerable<ILiteral> Literals { get; }
    }

    public interface ICnfFormula
    {
        IEnumerable<IClause> Clauses { get; }
    }

}
