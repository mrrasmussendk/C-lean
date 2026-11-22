using Gate.Contracts;
using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Builds the input of `gate check` from the files in the repository.</summary>
internal static class InputReader
{
    public static CheckInput Read(string root, ChangeSet change)
    {
        var config = Config(root);
        var architecture = ConfigText.Architecture(Files.Read(Layout.In(root, Layout.Architecture)));
        var revisions = new Revisions(
            SnapshotReader.Read(Layout.In(root, Layout.BaseOut), config.Languages),
            SnapshotReader.Read(Layout.In(root, Layout.HeadOut), config.Languages));
        return new CheckInput(config, architecture, change, revisions)
        {
            Records = Records(root, change),
            Protected = Protected(root),
        };
    }

    public static Config Config(string root)
    {
        var text = Files.Read(Layout.In(root, Layout.Config));
        return text.Length > 0 ? ConfigText.Config(text) : throw new GateFailure($"no {Layout.Config} in {root}; run `gate init`");
    }

    public static ChangeSet OfflineChange(string root) =>
        ChangeText.ChangeSet(Files.Read(Layout.In(root, Layout.ChangeSet)));

    private static IReadOnlyList<ChangeRecord> Records(string root, ChangeSet change) =>
        change.Commits.SelectMany(commit => commit.Paths)
            .Where(path => path.StartsWith(C1ChangeRecord.Directory) && path.EndsWith(".yaml"))
            .Distinct()
            .Where(path => Files.Exists(Layout.In(root, path)))
            .Select(path => ChangeText.Record(path, Files.Read(Layout.In(root, path))))
            .ToList();

    private static ProtectedData Protected(string root) =>
        new(ChangeText.Fingerprints(Files.Read(Layout.In(root, Layout.Baseline))), ChangeText.Fingerprints(Files.Read(Layout.In(root, Layout.Acknowledged))))
        {
            MutationIgnore = ChangeText.Patterns(Files.Read(Layout.In(root, Layout.MutationIgnore)), "ignore"),
            Waivers = ChangeText.Patterns(Files.Read(Layout.In(root, Layout.Waivers)), "waivers"),
            Lock = ChangeText.Lock(Files.Read(Layout.In(root, Layout.Lock))),
        };
}
