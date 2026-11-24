namespace Gate.Model;

internal sealed record Delta(IReadOnlyList<ComponentEdge> Add, IReadOnlyList<ComponentEdge> Remove);

internal sealed record Approval(string Approver, string At, string Digest);

/// <summary>One change.yaml (C1).</summary>
internal sealed record ChangeRecord(string Path, string Id, IReadOnlyList<string> Reasons, string Actor)
{
    public required Delta Delta { get; init; }

    public required IReadOnlyList<string> DeclaredTests { get; init; }

    /// <summary>The declared tests the change may delete; the rest of the declared tests it may only modify.</summary>
    public IReadOnlyList<string> DeletedTests { get; init; } = [];

    /// <summary>What the change must do, as its author and approver agreed. A reviewer decides each; the gate does not.</summary>
    public IReadOnlyList<string> Acceptance { get; init; } = [];

    public Approval? Approval { get; init; }
}

internal sealed record Commit(string Id, string Author, IReadOnlyList<string> Paths);

internal sealed record LineRange(string Path, int Start, int End);

/// <summary>What the change did to the repository: its commits, oldest first, and the lines it touched.</summary>
internal sealed record ChangeSet(IReadOnlyList<Commit> Commits, IReadOnlyList<LineRange> Lines)
{
    /// <summary>Earlier changes in the trailing window, for the single-responsibility report.</summary>
    public IReadOnlyList<PastChange> History { get; init; } = [];
}

internal sealed record PastChange(string Reason, string Actor, IReadOnlyList<string> Paths);
