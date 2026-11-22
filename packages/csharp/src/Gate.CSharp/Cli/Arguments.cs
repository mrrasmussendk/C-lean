using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;

namespace Gate.CSharp.Cli;

/// <summary>Reads the command line: analyze --root --out --config --architecture --today.</summary>
internal static class Arguments
{
    private const int PairSize = 2;
    private const string Usage =
        "Run: gate-csharp analyze --root <dir> --out <dir> --config <gate.yaml> --architecture <architecture.yaml> --today <YYYY-MM-DD>";

    private static readonly Regex DatePattern = new(@"^\d{4}-\d{2}-\d{2}$");
    private static readonly string[] Required = ["--root", "--out", "--config", "--architecture", "--today"];

    public static Options Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.FirstOrDefault() != "analyze")
        {
            throw new GateFailure(Failures.Line($"unknown command '{arguments.FirstOrDefault()}'", Usage));
        }
        var values = arguments.Skip(1).Chunk(PairSize).Where(pair => pair.Length == PairSize)
            .GroupBy(pair => pair[0]).ToDictionary(pair => pair.Key, pair => pair.Last()[1]);
        var missing = Required.Where(name => !values.ContainsKey(name)).ToList();
        if (missing.Count > 0)
        {
            throw new GateFailure(Failures.Line($"missing argument {string.Join(", ", missing)}", Usage));
        }
        if (!DatePattern.IsMatch(values["--today"]))
        {
            throw new GateFailure(Failures.Line($"--today is '{values["--today"]}', which is not a date", "Pass it as YYYY-MM-DD"));
        }
        return new Options(values["--root"], values["--out"], values["--config"], values["--architecture"], values["--today"]);
    }
}
