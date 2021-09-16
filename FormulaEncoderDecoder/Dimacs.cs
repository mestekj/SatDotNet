using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatDotNet.FormulaEncoderDecoder
{
    public class DimacsWriter
    {
        Dictionary<IVariable, int> dimacsVariables;

        public DimacsWriter()
        {
            dimacsVariables = new Dictionary<IVariable, int>();
        }

        public void WriteFormula(System.IO.TextWriter writer, ICnfFormula formula, IEnumerable<IVariable> originalVariables, IEnumerable<IVariable> tseitinVariables, IVariable root)
        {
            // Assign DIMACS' numbers to variables
            StringBuilder originalVariablesDescriptions = new StringBuilder();
            int dimacsVariable = 0;
            foreach(var variable in originalVariables)
            {
                dimacsVariable++;
                dimacsVariables.Add(variable, dimacsVariable);
                originalVariablesDescriptions.Append($"{variable.Name}: {dimacsVariable}, ");
            }
            originalVariablesDescriptions.Remove(originalVariablesDescriptions.Length - 2, 2); // Delete last ", "

            foreach (var variable in tseitinVariables)
            {
                dimacsVariable++;
                dimacsVariables.Add(variable, dimacsVariable);
            }

            // Write formula in DIMACS format
            int nVariables = dimacsVariable;
            int nClauses = formula.Clauses.Count();

            writer.WriteLine("c Encoding of original variables:");
            writer.WriteLine("c " + originalVariablesDescriptions);
            writer.WriteLine($"c Variable coresponding to the root node: {dimacsVariables[root]}");

            writer.WriteLine($"p cnf {nVariables} {nClauses}");

            foreach(var clause in formula.Clauses)
            {
                writer.WriteLine(EncodeClause(clause));
            }
        }

        private string EncodeClause(IClause clause)
        {
            StringBuilder dimacsClause = new StringBuilder();
            foreach(var literal in clause.Literals)
            {
                if (!literal.IsPositive)
                    dimacsClause.Append('-');
                dimacsClause.Append(dimacsVariables[literal.Variable]);
                dimacsClause.Append(' ');
            }
            dimacsClause.Append('0');
            return dimacsClause.ToString();
        }
    }

    public class DimacsReader
    {
        Dictionary<int, Variable> variables = new();
        public ICnfFormula ReadFormula(string dimacsFormula)
        {
            string[] lines = dimacsFormula.Split('\n');
            int lineToProcess = 0;

            // skip comments
            while (lines[lineToProcess].StartsWith("c"))
                lineToProcess++;

            var headTokens = lines[lineToProcess].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            int nvars = int.Parse(headTokens[2]);
            int nclauses = int.Parse(headTokens[3]);
            lineToProcess++;

            GenerateVariables(nvars);

            List<IClause> clauses = new List<IClause>();

            for (int i = 0; i < nclauses; i++)
            {
                var tokens = lines[lineToProcess].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                clauses.Add(ParseClause(tokens));

                lineToProcess++;
            }
            return new CnfFormula(clauses);
        }

        private void GenerateVariables(int nvars)
        {
            for (int i = 1; i <= nvars; i++)
            {
                variables.Add(i, new Variable(i.ToString()));
            }
        }

        private IClause ParseClause(string[] tokens)
        {
            List<ILiteral> literals = new List<ILiteral>();
            for (int i = 0; i < tokens.Length -1; i++)
            {
                int dimacsVariable = int.Parse(tokens[i]);
                IVariable variable = variables[Math.Abs(dimacsVariable)];
                ILiteral literal = dimacsVariable > 0 ? variable.PositiveLiteral : variable.NegativeLiteral;
                literals.Add(literal);
            }
            return new Clause(literals);
        }
    }
}
