namespace Gate.Model;

/// <summary>
/// A rule a repository adds to the gate: one file under .gate/rules. It finds its violations either with a
/// pattern matched against each line, or with a command that prints them.
/// </summary>
internal sealed record CustomRule(string Id, string Definition, Severity Severity, IReadOnlyList<string> Files)
{
    /// <summary>The line a rule command prints for each violation: `path:line: message`.</summary>
    public const string OutputLine = @"^(?<path>[^:]+):(?<line>\d+): (?<message>.+)$";

    /// <summary>A regular expression; a line it matches is a violation. Empty for a command rule.</summary>
    public string Pattern { get; init; } = "";

    /// <summary>A program that prints one `path:line: message` per violation. Empty for a pattern rule.</summary>
    public string Command { get; init; } = "";

    /// <summary>What is wrong with a line the pattern matches.</summary>
    public string Message { get; init; } = "";

    /// <summary>What to do about a finding.</summary>
    public string Fix { get; init; } = "";
}

internal sealed record SourceText(string Path, string Text);

/// <summary>What a custom rule is judged on: the files of the change it applies to, and what its command printed.</summary>
internal sealed record RuleEvidence(CustomRule Rule, IReadOnlyList<SourceText> Files, string Output);
