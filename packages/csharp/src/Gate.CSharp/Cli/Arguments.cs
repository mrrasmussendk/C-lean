using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;

namespace Gate.CSharp.Cli;

/// <summary>Reads the command line: a command, then --name value pairs and the flag --switch.</summary>
internal static class Arguments
{
    private const int PairSize = 2;
    private const string SwitchFlag = "--switch";
    private const string Common = "--root <dir> --config <gate.yaml> --architecture <architecture.yaml>";
    private const string Usage =
        $"Run one of: gate-csharp analyze {Common} --out <dir> --today <YYYY-MM-DD>; "
        + $"gate-csharp mutants {Common} --out <dir> [--lines <file>] [--switch]; "
        + $"gate-csharp instrument {Common} --trace-file <absolute path>";

    private static readonly Regex DatePattern = new(@"^\d{4}-\d{2}-\d{2}$");

    private static readonly IReadOnlyDictionary<string, string[]> Required = new Dictionary<string, string[]>
    {
        ["analyze"] = ["--root", "--config", "--architecture", "--out", "--today"],
        ["mutants"] = ["--root", "--config", "--architecture", "--out"],
        ["instrument"] = ["--root", "--config", "--architecture", "--trace-file"],
    };

    public static Options Parse(IReadOnlyList<string> arguments)
    {
        var command = arguments.FirstOrDefault("");
        var values = arguments.Skip(1).Where(argument => argument != SwitchFlag).Chunk(PairSize).Where(pair => pair.Length == PairSize)
            .GroupBy(pair => pair[0]).ToDictionary(pair => pair.Key, pair => pair.Last()[1]);
        var problem = Problems(command, values).FirstOrDefault();
        if (problem is not null)
        {
            throw new GateFailure(problem);
        }
        return new Options(
            command,
            values["--root"],
            values.GetValueOrDefault("--out", ""),
            values["--config"],
            values["--architecture"],
            values.GetValueOrDefault("--today", ""),
            values.GetValueOrDefault("--lines", ""),
            values.GetValueOrDefault("--trace-file", ""),
            arguments.Contains(SwitchFlag));
    }

    private static IEnumerable<string> Problems(string command, Dictionary<string, string> values)
    {
        if (!Required.TryGetValue(command, out var required))
        {
            return [Failures.Line($"unknown command '{command}'", Usage)];
        }
        var missing = required.Where(name => !values.ContainsKey(name)).ToList();
        var today = values.GetValueOrDefault("--today", "");
        var trace = values.GetValueOrDefault("--trace-file", "/");
        return new[]
        {
            (Wrong: missing.Count > 0, Line: Failures.Line($"missing argument {string.Join(", ", missing)}", Usage)),
            (Wrong: today != "" && !DatePattern.IsMatch(today), Line: Failures.Line($"--today is '{today}', which is not a date", "Pass it as YYYY-MM-DD")),
            (Wrong: !System.IO.Path.IsPathRooted(trace), Line: Failures.Line($"--trace-file is '{trace}', which is not an absolute path", "Pass an absolute path")),
        }.Where(check => check.Wrong).Select(check => check.Line);
    }
}
