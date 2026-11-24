using System.Text.RegularExpressions;
using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Loads the rules a repository adds under .gate/rules and gathers what each is judged on.</summary>
internal static class RuleFiles
{
    private const string Extension = ".yaml";
    private const double CommandSeconds = 120;
    private const int FoundViolations = 1;

    /// <summary>Every rule file, checked. A file that cannot be used fails the gate and says why.</summary>
    public static IReadOnlyList<CustomRule> Load(string root) =>
        Files.In(Path.Combine(root, Layout.Rules)).Where(path => path.EndsWith(Extension)).Select(Read).ToList();

    public static IReadOnlyList<RuleEvidence> Evidence(string root, Config config, ChangeSet change)
    {
        var excluded = config.Excluded.Concat(IgnoreLines(root)).Select(entry => entry.Trim('/')).SelectMany(entry => new[] { entry, $"{entry}/**" }).ToList();
        var touched = change.Lines.Select(range => range.Path).Distinct()
            .Where(path => Files.Exists(Path.Combine(root, path)) && !Glob.MatchesAny(excluded, path))
            .ToList();
        return Load(root).Where(rule => rule.Severity != Severities.Off)
            .Select(rule => (rule, files: touched.Where(path => Glob.MatchesAny(rule.Files, path)).ToList()))
            .Where(use => use.files.Count > 0)
            .Select(use => use.rule.Pattern.Length > 0
                ? new RuleEvidence(use.rule, use.files.Select(path => new SourceText(path, Files.Read(Path.Combine(root, path)))).ToList(), "")
                : new RuleEvidence(use.rule, [], Printed(root, use.rule, use.files)))
            .ToList();
    }

    private static CustomRule Read(string path)
    {
        Documents.Require(path, text => RuleText.Problems(Path.GetFileNameWithoutExtension(path), text));
        return Documents.Read(path, RuleText.Rule);
    }

    /// <summary>
    /// Runs a rule's command in the repository root with the files to judge on standard input, one per line.
    /// The command is a program and its arguments, split at spaces; a program with a slash is taken from the root.
    /// It exits 0 or 1 and prints one `path:line: message` per violation; anything else fails the gate.
    /// </summary>
    private static string Printed(string root, CustomRule rule, IReadOnlyList<string> files)
    {
        var words = rule.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var program = words[0].Contains('/') ? Files.Absolute(Path.Combine(root, words[0])) : words[0];
        var call = new ProcessCall(program, words.Skip(1).ToList(), root)
        {
            Input = string.Join('\n', files) + "\n",
            Variables = new Dictionary<string, string> { ["GATE_RULE"] = rule.Id },
            TimeoutSeconds = CommandSeconds,
        };
        var result = Processes.Run(call);
        var stray = result.Output.Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.Length > 0 && !Regex.IsMatch(line, CustomRule.OutputLine), "");
        if (result.ExitCode > FoundViolations || stray.Length > 0)
        {
            var reason = stray.Length > 0 ? $"printed a line that is not `path:line: message`: {stray}" : $"failed with exit code {result.ExitCode}: {result.Error.Trim()}";
            throw new GateFailure($"{Layout.Rules}/{rule.Id}{Extension}: the command `{rule.Command}` {reason}. A rule command exits 0 or 1 and prints one `path:line: message` per violation.");
        }

        return result.Output;
    }

    private static IEnumerable<string> IgnoreLines(string root) =>
        Files.Read(Path.Combine(root, Layout.Ignore)).Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#'));
}
