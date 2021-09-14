using SatDotNet.Core;
using SatDotNet.FormulaEncoderDecoder;
using SatDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SatDotNet.Dpll
{
    class Dpll
    {
        static Dictionary<string, string> ParseArgs(string[] args, string[] defaultKeys)
        {
            Dictionary<string, string> parsed = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--"))
                    // value only -> use default key
                    parsed.Add(defaultKeys[i], args[i]);
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
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
            var parsedArgs = ParseArgs(args, new string[] { "input", "output" });

            string input = null;
            bool inputIsDimacs;

            if (parsedArgs.TryGetValue("input", out string inputFile))
            {
                input = File.ReadAllText(inputFile);
                string extension = inputFile.Substring(inputFile.Length - 3);
                switch (extension)
                {
                    case "sat":
                        inputIsDimacs = false;
                        break;
                    case "cnf":
                        inputIsDimacs = true;
                        break;
                    default:
                        throw new ArgumentException($"File format .{extension} is not supported.");
                }
            }
            else
            {
                input = Console.ReadLine();
                inputIsDimacs = false;
            }


            ICnfFormula formula;
            if (inputIsDimacs)
            {
                var dimacsReader = new DimacsReader();
                formula = dimacsReader.ReadFormula(input);
            }
            else
            {
                var tseitinTranslator = new TseitinTranslator(parsedArgs.ContainsKey("equivalence"));
                formula = tseitinTranslator.TranslateFormula(input, out IVariable rootVariable);
            }

            var solver = new Solver();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var solvedFormula = solver.Solve(formula);
            stopWatch.Stop();


            if (solvedFormula.IsUnsatisfiable)
                Console.WriteLine("UNSAT");
            else
            {
                Console.WriteLine("SAT");
                Console.WriteLine();
                var literals =
                    solvedFormula.GetAssignment()
                    .Where(l => l.Variable.Name != null)
                    .OrderBy(l => l.Variable.Name)
                    .Select(l => (l.IsPositive ? "" : "~ ") + l.Variable.Name);
                Console.WriteLine("Model:");
                Console.WriteLine(String.Join(", ", literals));
            }

            Console.WriteLine();
            Console.WriteLine("Solving statistics:");
            Console.WriteLine($"Soving time: {stopWatch.Elapsed}, decisions: {solver.DecisionHeuristic.DecisionsCount}, unit propagation steps: {solvedFormula.UnitPropagationSteps}");
        }
    }
}
