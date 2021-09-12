using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatDotNet.FormulaEncoderDecoder
{

    abstract class NnfNode : IVisitable
    {
        public abstract T Accept<T>(IVisitor<T> visitor);
    }

    class VariableNnfNode : NnfNode
    {
        public VariableNnfNode(IVariable variable)
        {
            Variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        public IVariable Variable { get; }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitNode(this);
        }
    }

    class NegationNnfNode : NnfNode
    {
        public NegationNnfNode(VariableNnfNode child)
        {
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public NegationNnfNode(IVariable variable)
        {
            Child = new VariableNnfNode(variable);
        }

        public VariableNnfNode Child { get; }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitNode(this);
        }
    }

    abstract class BinaryNnfNode : NnfNode
    {
        public BinaryNnfNode(NnfNode left, NnfNode right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public NnfNode Left { get; }
        public NnfNode Right { get; }
    }

    class OrNnfNode : BinaryNnfNode
    {
        public OrNnfNode(NnfNode left, NnfNode right) : base(left, right)
        {
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitNode(this);
        }
    }

    class AndNnfNode : BinaryNnfNode
    {
        public AndNnfNode(NnfNode left, NnfNode right) : base(left, right)
        {
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitNode(this);
        }
    }

    class NnfTree : IVisitor<IEnumerable<NnfNode>>
    {
        public NnfTree(NnfNode root, IEnumerable<Variable> variables)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Variables = variables ?? throw new ArgumentNullException(nameof(variables));
            Nodes = GetNodes(root);
        }

        private IEnumerable<NnfNode> GetNodes(NnfNode root)
        {
            return root.Accept(this);
        }

        IEnumerable<NnfNode> IVisitor<IEnumerable<NnfNode>>.VisitNode(AndNnfNode node)
        {
            yield return node;
            foreach (var subnode in GetNodes(node.Left))
                yield return subnode;
            foreach (var subnode in GetNodes(node.Right))
                yield return subnode;
        }

        IEnumerable<NnfNode> IVisitor<IEnumerable<NnfNode>>.VisitNode(OrNnfNode node)
        {
            yield return node;
            foreach (var subnode in GetNodes(node.Left))
                yield return subnode;
            foreach (var subnode in GetNodes(node.Right))
                yield return subnode;
        }

        IEnumerable<NnfNode> IVisitor<IEnumerable<NnfNode>>.VisitNode(NegationNnfNode node)
        {
            yield return node;
            yield return node.Child;
        }

        IEnumerable<NnfNode> IVisitor<IEnumerable<NnfNode>>.VisitNode(VariableNnfNode node)
        {
            yield return node;
        }

        public NnfNode Root { get; }
        public IEnumerable<NnfNode> Nodes { get; }
        public IEnumerable<Variable> Variables { get; }
    }

    interface IVisitor<T>
    {
        T VisitNode(AndNnfNode node);
        T VisitNode(OrNnfNode node);
        T VisitNode(NegationNnfNode node);
        T VisitNode(VariableNnfNode node);
    }

    interface IVisitable
    {
        T Accept<T>(IVisitor<T> visitor);
    }
}
