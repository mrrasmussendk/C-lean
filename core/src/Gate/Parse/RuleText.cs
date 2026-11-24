using System.Text.RegularExpressions;
using Gate.Model;

namespace Gate.Parse;

/// <summary>Reads one rule file of .gate/rules, and says what is wrong with one that cannot be used.</summary>
internal static class RuleText
{
    private const string IdForm = "^[A-Z][A-Z0-9-]*$";
    private static readonly IReadOnlyList<string> Severities = ["block", "warn", "report", "off"];

    public static CustomRule Rule(string text)
    {
        var root = Yaml.Root(text);
        return new CustomRule(Yaml.Text(root, "id"), Yaml.Text(root, "definition"), Model.Severities.Named(Yaml.Text(root, "severity")), Yaml.Texts(root, "files"))
        {
            Pattern = Yaml.Text(root, "pattern"),
            Command = Yaml.Text(root, "command"),
            Message = Yaml.Text(root, "message"),
            Fix = Yaml.Text(root, "fix"),
        };
    }

    /// <summary>What is wrong with a rule file that parses, each as `line: reason`. The file is named after the rule.</summary>
    public static IReadOnlyList<string> Problems(string name, string text)
    {
        var root = Yaml.Root(text);
        var rule = Rule(text);
        return new[]
        {
            rule.Id != name ? $"`id` is '{rule.Id}' but the file is named {name}.yaml. Give both the same rule ID." : "",
            !Regex.IsMatch(name, IdForm) ? $"the rule ID '{name}' must be capital letters, digits and hyphens, starting with a letter, as in NO-TODO." : "",
            RuleTable.Entries.Any(entry => entry.Id == name) ? $"'{name}' is a rule of the gate itself. Choose another ID; to change that rule, set it under `rules:` in gate.yaml." : "",
            rule.Definition.Length == 0 ? "`definition` is missing. Say in one sentence what the rule asks for." : "",
            !Severities.Contains(Yaml.Text(root, "severity")) ? "`severity` must be block, warn, report or off." : "",
            rule.Files.Count == 0 ? "`files` is missing. List the globs the rule applies to, as in [\"**/*.cs\"]." : "",
            (rule.Pattern.Length == 0) == (rule.Command.Length == 0) ? "give exactly one of `pattern` (a regular expression matched against each line) and `command` (a program that prints `path:line: message`)." : "",
            rule.Pattern.Length > 0 && rule.Message.Length == 0 ? "`message` is missing. A pattern rule says in `message` what is wrong with a line it matches." : "",
            PatternProblem(rule.Pattern),
        }
        .Where(problem => problem.Length > 0).Select(problem => $"1: {problem}").ToList();
    }

    private static string PatternProblem(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
            return "";
        }
        catch (ArgumentException invalid)
        {
            return $"`pattern` is not a regular expression: {invalid.Message}";
        }
    }
}
