namespace Gate.Model;

/// <summary>One line of the gate's own log: what one check cost and what it found.</summary>
internal sealed record CheckRun(string At, string Mode, string Change, double Seconds)
{
    public required int ExitCode { get; init; }

    /// <summary>Findings that stop a merge (block or warn), counted per rule.</summary>
    public required IReadOnlyDictionary<string, int> Findings { get; init; }
}
