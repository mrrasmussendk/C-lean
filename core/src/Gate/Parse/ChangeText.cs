using Gate.Model;
using YamlDotNet.RepresentationModel;

namespace Gate.Parse;

/// <summary>change.yaml, the change set, and the protected lists.</summary>
internal static class ChangeText
{
    public static ChangeRecord Record(string path, string text)
    {
        var root = Yaml.Root(text);
        var delta = Yaml.Map(root, "delta");
        var tests = Yaml.Map(root, "tests");
        return new ChangeRecord(path, Yaml.Text(root, "id"), Yaml.Texts(root, "reason"), Yaml.Text(root, "actor"))
        {
            Delta = new Delta(ConfigText.Edges(delta, "add"), ConfigText.Edges(delta, "remove")),
            DeclaredTests = TestNames(tests, "modify").Concat(TestNames(tests, "delete")).ToList(),
            Acceptance = Yaml.Texts(root, "acceptance"),
            Approval = Approval(Yaml.Map(root, "approval")),
        };
    }

    public static ChangeSet ChangeSet(string text)
    {
        var root = Yaml.Root(text);
        var commits = Yaml.Maps(root, "commits").Select(Commit).ToList();
        var lines = Yaml.Maps(root, "lines").Select(Range).ToList();
        return new ChangeSet(commits, lines)
        {
            History = Yaml.Maps(root, "history").Select(past => new PastChange(Yaml.Text(past, "reason"), Yaml.Text(past, "actor"), Yaml.Texts(past, "paths"))).ToList(),
        };
    }

    public static IReadOnlySet<string> Fingerprints(string text) =>
        Yaml.Texts(Yaml.Root(text), "fingerprints").ToHashSet();

    public static IReadOnlyList<MutantPattern> Patterns(string text, string key) =>
        Yaml.Maps(Yaml.Root(text), key).Select(Pattern).ToList();

    public static IReadOnlyList<Verdict> Lock(string text) =>
        Yaml.Maps(Yaml.Root(text), "verdicts").Select(Verdict).ToList();

    private static IReadOnlyList<string> TestNames(YamlMappingNode tests, string key) =>
        Yaml.Maps(tests, key).Select(entry => Yaml.Text(entry, "test")).ToList();

    private static Approval? Approval(YamlMappingNode map) =>
        Yaml.Text(map, "approver").Length == 0
            ? null
            : new Approval(Yaml.Text(map, "approver"), Yaml.Text(map, "at"), Yaml.Text(map, "digest"));

    private static Commit Commit(YamlMappingNode map) =>
        new(Yaml.Text(map, "id"), Yaml.Text(map, "author"), Yaml.Texts(map, "paths"));

    private static LineRange Range(YamlMappingNode map) =>
        new(Yaml.Text(map, "path"), Yaml.Number(map, "start"), Yaml.Number(map, "end"));

    private static MutantPattern Pattern(YamlMappingNode map) =>
        new(Yaml.Text(map, "path").Length > 0 ? Yaml.Text(map, "path") : "**", Yaml.Text(map, "mutator").Length > 0 ? Yaml.Text(map, "mutator") : "*")
        {
            Context = Yaml.Text(map, "context"),
        };

    private static Verdict Verdict(YamlMappingNode map) =>
        new(Yaml.Text(map, "hash"), Yaml.Text(map, "question"), Yaml.Text(map, "fingerprint"), Yaml.Text(map, "split"));
}
