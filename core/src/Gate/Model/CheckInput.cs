namespace Gate.Model;

internal sealed record MutantPattern(string Path, string Mutator)
{
    /// <summary>`logging` or `guard`; empty matches a mutant in any context.</summary>
    public string Context { get; init; } = "";
}

internal sealed record Verdict(string Hash, string Question, string Fingerprint, string Split);

/// <summary>The protected files a human controls: baseline, acknowledgements, ignore list, waivers, lockfile.</summary>
internal sealed record ProtectedData(IReadOnlySet<string> Baseline, IReadOnlySet<string> Acknowledged)
{
    public required IReadOnlyList<MutantPattern> MutationIgnore { get; init; }

    public required IReadOnlyList<MutantPattern> Waivers { get; init; }

    public required IReadOnlyList<Verdict> Lock { get; init; }
}

internal sealed record Revisions(Snapshot Base, Snapshot Head);

/// <summary>Everything `c-lean check` decides on. Built once by the collectors; every stage is a function of it.</summary>
internal sealed record CheckInput(Config Config, Architecture Architecture, ChangeSet Change, Revisions Revisions)
{
    public required IReadOnlyList<ChangeRecord> Records { get; init; }

    public required ProtectedData Protected { get; init; }
}
