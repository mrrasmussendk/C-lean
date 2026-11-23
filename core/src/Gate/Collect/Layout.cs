namespace Gate.Collect;

/// <summary>Where the gate's files are, relative to the repository root. Pure path arithmetic.</summary>
internal static class Layout
{
    public const string Config = "gate.yaml";
    public const string Architecture = "architecture.yaml";
    public const string Lock = "gate.lock";
    public const string ChangeSet = ".gate/changeset.yaml";
    public const string Baseline = ".gate/baseline.yaml";
    public const string Acknowledged = ".gate/acks.yaml";
    public const string MutationIgnore = ".gate/mutation-ignore.yaml";
    public const string Waivers = ".gate/mutation-waivers.yaml";
    public const string HeadOut = ".gate/out";
    public const string BaseOut = ".gate/base";
    public const string Nightly = ".gate/nightly";
    public const string TraceEdges = "trace-edges.jsonl";
    public const string EditOut = ".gate/edit";
    public const string Attempts = ".gate/edit/attempts.tsv";
    public const string Log = ".gate/log/checks.jsonl";
    public const string Conflicts = "conflicts";
    public const string AgentInstructions = "CLAUDE.md";

    public const string Records = "changes";

    public static string Record(string id) => $"{Records}/{id}.yaml";
}
