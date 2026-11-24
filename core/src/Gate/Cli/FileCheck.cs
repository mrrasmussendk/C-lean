using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Cli;

/// <summary>What the check of one file found, ready to show.</summary>
internal sealed record FileResult(int ExitCode, IReadOnlyList<string> Lines);

/// <summary>
/// `c-lean check --file &lt;path&gt;`: the rules of one language on the working tree, reported for one file.
/// It is the inner loop: seconds after an edit, not minutes before a merge. CI remains the gate.
/// </summary>
internal static class FileCheck
{
    public const string Option = "--file";

    private static readonly IReadOnlyList<(string Language, string Extension)> Known = [("csharp", ".cs"), ("typescript", ".ts"), ("typescript", ".tsx"), ("typescript", ".mts"), ("typescript", ".cts"), ("python", ".py")];

    public static int Run(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var result = Check(root, Options.Value(arguments, Option, ""));
        foreach (var line in result.Lines)
        {
            Surroundings.Print(line);
        }

        return result.ExitCode;
    }

    public static FileResult Check(string root, string path)
    {
        var started = Surroundings.Seconds();
        var file = Files.Relative(root, path);
        var config = InputReader.Config(root);
        var language = Known.Where(known => file.EndsWith(known.Extension)).Select(known => known.Language).FirstOrDefault("");
        if (!config.Languages.Contains(language))
        {
            return new FileResult(C6Hardening.Passed, []);
        }

        NativeRunner.AnalyzeEdit(root, config, language);
        var found = Documents.Read(Path.Combine(root, Layout.EditOut, language, "findings.sarif"), InterchangeText.Findings);
        var findings = C6Hardening.Weighed(InputReader.Protected(root), Settings.Enforced(config, found.Where(finding => finding.Where.File == file).ToList()));
        var exitCode = findings.Any(C6Hardening.Stops) ? C6Hardening.Blocked : C6Hardening.Passed;
        CheckLog.Write(root, new Measured(Stats.PerFile, Surroundings.Seconds() - started, exitCode), findings);
        return new FileResult(exitCode, [.. Report.Lines(root, findings), .. Advice(root, file, findings), $"c-lean: {file}: {Report.Counts(findings)}"]);
    }

    /// <summary>Counts how many edits each stopping finding has survived, and says when it is time to file a conflict.</summary>
    private static IReadOnlyList<string> Advice(string root, string file, IReadOnlyList<Finding> findings)
    {
        var path = Path.Combine(root, Layout.Attempts);
        var state = Files.Read(path);
        var counts = Attempts.Next(state, findings.Where(C6Hardening.Stops).Select(finding => finding.Fingerprint).ToList());
        Files.Write(path, Attempts.State(state, file, counts));
        return Attempts.Advice(counts);
    }
}
