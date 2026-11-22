namespace Gate.Model;

internal sealed record RuleSetting(int Limit, string Severity);

internal sealed record TestSettings(double RuntimeBudgetSeconds, string MissingReport)
{
    /// <summary>Per language: the project's own command that runs its tests and writes JUnit XML to $GATE_JUNIT.</summary>
    public required IReadOnlyDictionary<string, string> Commands { get; init; }

    /// <summary>Per language: a command run once in a fresh checkout before any test run (restore, install).</summary>
    public required IReadOnlyDictionary<string, string> Setup { get; init; }
}

internal sealed record JudgeSettings(int Votes, string Model, IReadOnlyList<string> Command, string FailHistory);

internal sealed record Config(
    string Spec,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> Implementers,
    IReadOnlyDictionary<string, RuleSetting> Rules,
    IReadOnlyList<string> Protected)
{
    public required TestSettings Tests { get; init; }

    public required JudgeSettings Judge { get; init; }

    public required IReadOnlyDictionary<string, string> Commands { get; init; }
}

internal sealed record Component(string Name, IReadOnlyList<string> Paths, bool Effect, bool Boundary)
{
    public bool CoreLogic { get; init; }

    /// <summary>Its behaviour must be replaceable: outside code reaches it through an abstraction only.</summary>
    public bool Varies { get; init; }

    /// <summary>It may construct the implementations of varying components: the place where the program is put together.</summary>
    public bool Wires { get; init; }
}

internal sealed record ComponentEdge(string From, string To);

internal sealed record Architecture(IReadOnlyList<Component> Components, IReadOnlyList<ComponentEdge> Edges);
