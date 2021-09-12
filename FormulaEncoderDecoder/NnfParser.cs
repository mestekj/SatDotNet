using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SatDotNet.FormulaEncoderDecoder
{
    class NnfParser
    {
        public Dictionary<string, Variable> Variables { get; }

        public NnfParser()
        {
            Variables = new Dictionary<string, Variable>();
        }

        protected Variable GetVariable(string name)
        {
            if(!Variables.ContainsKey(name)) {
                Variable variable = new Variable(name);
                Variables.Add(name, variable);
            }
            return Variables[name];
        }

        public NnfTree ParseFormula(string formula)
        {
            //ensure whitespaces around parenthesis
            formula = formula.Replace("(", " ( ").Replace(")", " ) ");

            //process
            string[] tokens = formula.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            int firstNotProcessedToken = 0;
            NnfNode parsed = ParseFormula(tokens,ref firstNotProcessedToken);
            //TODO check returned values
            if (firstNotProcessedToken != tokens.Length)
                throw new ArgumentException($"Formula processed but {tokens.Length - firstNotProcessedToken} tokens remained not processed.");
            
            return new NnfTree(parsed, Variables.Values);
        }

        protected NnfNode ParseFormula(string[] tokens, ref int firstNotProcessedToken)
        {
            NnfNode node;
            switch (tokens[firstNotProcessedToken])
            {
                case "(":
                    firstNotProcessedToken += 1; // (
                    string operatorToken = tokens[firstNotProcessedToken];
                    firstNotProcessedToken += 1; // operatorToken

                    switch (operatorToken)
                    {
                        case "and":
                            NnfNode left = ParseFormula(tokens, ref firstNotProcessedToken);
                            NnfNode right = ParseFormula(tokens, ref firstNotProcessedToken);
                            node = new AndNnfNode(left, right);
                            break;
                        case "or":
                            left = ParseFormula(tokens, ref firstNotProcessedToken);
                            right = ParseFormula(tokens, ref firstNotProcessedToken);
                            node = new OrNnfNode(left, right);
                            break;
                        case "not":
                            IVariable variable = GetVariable(tokens[firstNotProcessedToken]);
                            node = new NegationNnfNode(variable);
                            firstNotProcessedToken += 1; // `variable` token
                            break;
                        default:
                            throw new ArgumentException($"Unsupported NNF operator '{operatorToken}', token {firstNotProcessedToken - 1}");
                    }

                    if (tokens[firstNotProcessedToken] != ")")
                        throw new ArgumentException($"Unexpected token {firstNotProcessedToken}: expected ')', found '{tokens[firstNotProcessedToken]}'");
                    firstNotProcessedToken += 1; // )
                    break;

                default:
                    string variableToken = tokens[firstNotProcessedToken];
                    node = new VariableNnfNode(GetVariable(variableToken));
                    firstNotProcessedToken += 1; // variableToken
                    break;
            }

            return node;
        }
    }
}
