# SatDotNet
SAT Solver implemented in C# for .NET 5

## Spuštění programů
Pro spuštění je potřeba .NET 5.0, instalace pro Linux [zde](https://docs.microsoft.com/en-us/dotnet/core/install/linux).

Program se zkompiluje spuštěním příkazu `dotnet build` v adresáři příslušného projektu (viz níže). 
Zkompilovaný program je pak spustitelný soubor `<adresář s projektem>/bin/Debug/net5.0/<název projektu>`.

## Úloha 1 - Tseitin encoding
Úlohu řeší program `SatDotNet.Formula2Cnf` (projekt `SatDotNet.Formula2Cnf`).

Vstupní a výstupní soubor lze nastavit pomocí command-line argumentů, a to dvěma způsoby:
```
SatDotNet.Formula2Cnf <input_file> <output_file>
```
nebo
```
SatDotNet.Formula2Cnf --input <input_file> --output <output_file>
```
Při nenastavení souborů se použije standardní vstup/výstup.

Program standardně používá jen implikace, použití ekvivalencí lze zapnout přidáním optionu `--equivalence`.

## Úloha 2 - DPLL
Úlohu řeší program `SatDotNet.Dpll` (projekt `SatDotNet.Dpll`).

Vstup může být čten ze souboru, opět jsou možné dva způsoby
(`SatDotNet.Dpll <input_file>` nebo `SatDotNet.Dpll --input <input_file>`),
formát vstupu je určen podle přípony vstupního souboru.

Pokud není nastaven vstupní soubor, program čte ze standardního vstupu, a to 1 řádek ve zjednodušeném formátu SMT-LIB.
Opět lze použít option `--equivalence` pro použití ekvivalencí.

Výstup programu je vždy psán na standardní výstup.

Speciální způsob spuštění programu je
```
SatDotNet.Dpll --input-dir <dir>
```
V tomto případě se solver postupně spustí na všechny soubory `*.cnf` v daném adresáři a výsledky zapíše do souboru `results.csv` tamtéž.

## Úloha 3 - Watched Literals
Úloha je řešena stejným programem jako úloha 2, tj. projektem `SatDotNet.Dpll`.

Použití watched literals místo adjacency lists se zapne přidáním optionu `--watched-literals`.