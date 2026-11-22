namespace Gate.Model;

/// <summary>How much a finding weighs. A higher rank stops more.</summary>
internal sealed record Severity(string Name, int Rank);

internal static class Severities
{
    private const int BlockRank = 2;

    public static readonly Severity Report = new("report", 0);
    public static readonly Severity Warn = new("warn", 1);
    public static readonly Severity Block = new("block", BlockRank);

    public static Severity Named(string name) =>
        name == Block.Name || name == "error" ? Block
        : name == Warn.Name || name == "warning" ? Warn
        : Report;
}

internal sealed record Location(string File, int Line);

internal sealed record Finding(string Rule, Severity Severity, Location Where, string Message)
{
    public string Fingerprint { get; init; } = "";
}

internal static class Blocking
{
    /// <summary>A blocking finding of a contract, fingerprinted by where it is.</summary>
    public static Finding At(string rule, Location where, string message) =>
        new(rule, Severities.Block, where, $"{rule}: {message}")
        {
            Fingerprint = $"{rule}|{where.File}|{where.Line}|{message}",
        };
}
