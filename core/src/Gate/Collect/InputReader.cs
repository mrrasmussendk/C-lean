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
        Documents.Require(Layout.In(root, Layout.Architecture), Malformed.Architecture);
        var architecture = Documents.Read(Layout.In(root, Layout.Architecture), ConfigText.Architecture);
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
        var path = Layout.In(root, Layout.Config);
        if (!Files.Exists(path))
        {
            throw new GateFailure($"{path}: the file does not exist. Run `gate init` in the repository root, or pass --root.");
        }

        Documents.Require(path, Malformed.Config);
        return Documents.Read(path, ConfigText.Config);
    }

    public static ChangeSet OfflineChange(string root) =>
        Documents.Read(Layout.In(root, Layout.ChangeSet), ChangeText.ChangeSet);

    private static IReadOnlyList<ChangeRecord> Records(string root, ChangeSet change) =>
        change.Commits.SelectMany(commit => commit.Paths)
            .Where(path => path.StartsWith(C1ChangeRecord.Directory) && path.EndsWith(".yaml"))
            .Distinct()
            .Where(path => Files.Exists(Layout.In(root, path)))
            .Select(path => Documents.Read(Layout.In(root, path), text => ChangeText.Record(path, text)))
            .ToList();

    private static ProtectedData Protected(string root) =>
        new(Documents.Read(Layout.In(root, Layout.Baseline), ChangeText.Fingerprints), Documents.Read(Layout.In(root, Layout.Acknowledged), ChangeText.Fingerprints))
        {
            MutationIgnore = Documents.Read(Layout.In(root, Layout.MutationIgnore), text => ChangeText.Patterns(text, "ignore")),
            Waivers = Documents.Read(Layout.In(root, Layout.Waivers), text => ChangeText.Patterns(text, "waivers")),
            Lock = Documents.Read(Layout.In(root, Layout.Lock), ChangeText.Lock),
        };
}
