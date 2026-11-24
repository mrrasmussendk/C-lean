using Gate.Contracts;
using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

/// <summary>
/// The rules in words: the block `c-lean init` writes under `rules:` in gate.yaml, and the table of `c-lean rules`.
/// Both exist so that a reader of the configuration never has to know what an ID such as DC03 stands for.
/// </summary>
internal static class RuleCatalogue
{
    /// <summary>The groups of the spec's rule table, by the two letters a rule ID starts with.</summary>
    private static readonly IReadOnlyList<(string Prefix, string Heading)> Groups =
    [
        ("SZ", "Size and shape"), ("AR", "Arguments"), ("FX", "Side effects"), ("DU", "Duplication"),
        ("ER", "Error handling"), ("CP", "Coupling"), ("DC", "Dead code"), ("FR", "Fragmentation"),
        ("NM", "Naming"), ("TS", "Tests"), ("HD", "Hardening"), ("GR", "Call graph"),
    ];

    private const string Header = """
        # Every rule the gate enforces, each under a line that says what it asks for.
        # To change a rule, edit its line:
        #   limit      the number the rule measures against
        #   severity   block   the change cannot merge
        #              warn    a human must acknowledge it
        #              report  recorded, never stops a change
        #              off     the rule is not enforced
        # SLOC means lines of code: blank lines and comments are not counted.
        # `c-lean rules` shows what applies now; `c-lean explain <ID>` says how to fix a finding.
        rules:
        """;

    /// <summary>`c-lean rules`: every rule with what it asks for and how this repository has it set.</summary>
    public static int Run(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var config = Files.Exists(Path.Combine(root, Collect.Layout.Config)) ? Collect.InputReader.Config(root) : Collect.InputReader.Unpinned;
        foreach (var line in Table(config, Guide.Pen()))
        {
            Surroundings.Print(line);
        }

        return C6Hardening.Passed;
    }

    /// <summary>The `rules:` block of a new gate.yaml: every rule at its default, under its definition.</summary>
    public static string Yaml() =>
        string.Join('\n', Header.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').Concat(Grouped().SelectMany(YamlGroup)));

    private static IEnumerable<string> YamlGroup((string Heading, IReadOnlyList<RuleEntry> Rules) group) =>
        new[] { "", $"  # ---- {group.Heading} ----" }
            .Concat(group.Rules.SelectMany(rule => new[] { $"  # {rule.Definition}", $"  {rule.Id}: {{{YamlSetting(rule)}}}" }));

    private static string YamlSetting(RuleEntry rule) =>
        HasLimit(rule) ? $"limit: {DefaultLimit(rule)}, severity: {rule.Severity.Name}" : $"severity: {rule.Severity.Name}";

    private static IReadOnlyList<string> Table(Config config, Pen pen) =>
    [
        pen.Paint("Rules in this repository", Art.Bold),
        "Set in gate.yaml under `rules:`. `c-lean explain <ID>` says how to fix a finding.",
        .. Grouped().SelectMany(group => new[] { "", pen.Paint(group.Heading, Art.Bold) }.Concat(group.Rules.Select(rule => Row(config, rule, pen)))),
    ];

    private static string Row(Config config, RuleEntry rule, Pen pen)
    {
        var severity = Settings.SwitchedOff(config, rule.Id) ? Severities.Off : Settings.Severity(config, rule.Id);
        var limit = HasLimit(rule) ? $"limit {Settings.Limit(config, rule.Id)}" : "";
        var changed = severity == rule.Severity && limit == (HasLimit(rule) ? $"limit {DefaultLimit(rule)}" : "") ? "" : "  (changed in gate.yaml)";
        return $"  {rule.Id}  {pen.Paint($"{severity.Name,-SeverityWidth}", Colour(severity))}  {limit,-LimitWidth}  {rule.Definition}{changed}";
    }

    private static string Colour(Severity severity) =>
        severity == Severities.Block ? Art.Red : severity == Severities.Warn ? Art.Yellow : Art.Cyan;

    private static IEnumerable<(string Heading, IReadOnlyList<RuleEntry> Rules)> Grouped() =>
        Groups.Select(group => (group.Heading, Rules: (IReadOnlyList<RuleEntry>)RuleTable.Entries.Where(rule => rule.Id.StartsWith(group.Prefix)).ToList()))
            .Where(group => group.Rules.Count > 0);

    /// <summary>A rule has a limit when its default in the spec is a number, as in `30` or `6 statements`.</summary>
    private static bool HasLimit(RuleEntry rule) => char.IsDigit(rule.Default[0]);

    private static string DefaultLimit(RuleEntry rule) => new(rule.Default.TakeWhile(char.IsDigit).ToArray());

    private const int SeverityWidth = 6;
    private const int LimitWidth = 9;
}
