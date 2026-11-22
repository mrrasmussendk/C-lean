namespace Gate.Model;

internal sealed record Node(string Id, string Kind, string Component, string File)
{
    public bool Test { get; init; }

    public bool Effect { get; init; }

    public bool Property { get; init; }

    public int Line { get; init; }
}

internal sealed record Edge(string From, string To, string Kind);

internal sealed record Graph(IReadOnlyList<Node> Nodes, IReadOnlyList<Edge> Edges);

internal sealed record Sequence(string Hash, int StartLine, int EndLine);

internal sealed record Unit(string Id, string File, string Hash, IReadOnlyList<Sequence> Sequences)
{
    public int StartLine { get; init; }

    public int EndLine { get; init; }

    public bool Test { get; init; }

    public int Callers { get; init; }
}

internal sealed record Mutant(string File, int Line, string Mutator, string Status)
{
    public required IReadOnlyList<string> KilledBy { get; init; }

    public string Context { get; init; } = "";
}

internal sealed record TestRun(string Name, double Seconds)
{
    public bool Failed { get; init; }
}

/// <summary>One mutant a native package proposes: replace bytes [Start, End) of the file with the replacement.</summary>
internal sealed record MutantPlan(string Id, string File, string Mutator, string Replacement)
{
    public int Line { get; init; }

    public int Column { get; init; }

    public int Start { get; init; }

    public int End { get; init; }

    public string Context { get; init; } = "";
}

internal sealed record MutantResult(MutantPlan Plan, string Status, IReadOnlyList<string> KilledBy);

internal sealed record Span(string Id, string Parent, string Component);

/// <summary>Everything the native packages wrote for one revision, all languages merged.</summary>
internal sealed record Snapshot(IReadOnlyList<Finding> Findings, Graph Graph, IReadOnlyList<Unit> Units)
{
    public required bool HasMutationReport { get; init; }

    public required IReadOnlyList<Mutant> Mutants { get; init; }

    public required IReadOnlyList<TestRun> Tests { get; init; }

    public required IReadOnlyList<Span> Spans { get; init; }
}
