using SatDotNet.FormulaEncoderDecoder;
using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace SatDotNet.Formula2Cnf
{
    class Formula2Cnf
    {
        static Dictionary<string, string> ParseArgs(string[] args)
        {
            Dictionary<string, string> parsed = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                if(i+1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    // key-value
                    parsed.Add(args[i].Remove(0, 2), args[i + 1]);
                    i++; //skip value
                }
                else
                    // key only
                    parsed.Add(args[i].Remove(0, 2), "");
            }
            return parsed;
        }

        static void Main(string[] args)
        {
            var parsedArgs = ParseArgs(args);

            string input = null;
            TextWriter output = Console.Out;

            if(parsedArgs.TryGetValue("input", out string inputFile))
            {
                input = File.ReadAllText(inputFile);
            }
            else
            {
                input = Console.ReadLine();
            }

            if (parsedArgs.TryGetValue("output", out string outputFile))
            {
                output = File.CreateText(outputFile);
            }

            var tseitinTranslator = new TseitinTranslator(parsedArgs.ContainsKey("equivalence"));
            ICnfFormula formula = tseitinTranslator.TranslateFormula(input, out IVariable rootVariable);
            var dimacsWriter = new DimacsWriter();
            dimacsWriter.WriteFormula(output, formula, tseitinTranslator.GetOriginalVariables(), tseitinTranslator.GetAuxiliaryVariables(), rootVariable);

            output.Close();
        }
    }
}
