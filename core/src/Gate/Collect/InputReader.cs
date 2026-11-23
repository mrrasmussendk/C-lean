using Gate.Contracts;
using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Builds the input of `c-lean check` from the files in the repository.</summary>
internal static class InputReader
{
    public static CheckInput Read(string root, ChangeSet change)
    {
        var config = Config(root);
        Documents.Require(Path.Combine(root, Layout.Architecture), Malformed.Architecture);
        var architecture = Documents.Read(Path.Combine(root, Layout.Architecture), ConfigText.Architecture);
        var revisions = new Revisions(
            SnapshotReader.Read(Path.Combine(root, Layout.BaseOut), config.Languages),
            SnapshotReader.Read(Path.Combine(root, Layout.HeadOut), config.Languages));
        return new CheckInput(config, architecture, change, revisions)
        {
            Records = Records(root, change),
            Protected = Protected(root),
        };
    }

    /// <summary>A configuration that names no package command, for finding the installed packages before there is a gate.yaml.</summary>
    public static readonly Config Unpinned = ConfigText.Config("");

    public static Config Config(string root)
    {
        var path = Path.Combine(root, Layout.Config);
        if (!Files.Exists(path))
        {
            throw new GateFailure($"{path}: the file does not exist. Run `c-lean init` in the repository root, or pass --root.");
        }

        Documents.Require(path, Malformed.Config);
        return Documents.Read(path, ConfigText.Config);
    }

    public static ChangeSet OfflineChange(string root) =>
        Documents.Read(Path.Combine(root, Layout.ChangeSet), ChangeText.ChangeSet);

    private static IReadOnlyList<ChangeRecord> Records(string root, ChangeSet change) =>
        change.Commits.SelectMany(commit => commit.Paths)
            .Where(path => path.StartsWith(C1ChangeRecord.Directory) && path.EndsWith(".yaml"))
            .Distinct()
            .Where(path => Files.Exists(Path.Combine(root, path)))
            .Select(path => Documents.Read(Path.Combine(root, path), text => ChangeText.Record(path, text)))
            .ToList();

    public static ProtectedData Protected(string root) =>
        new(Documents.Read(Path.Combine(root, Layout.Baseline), ChangeText.Fingerprints), Documents.Read(Path.Combine(root, Layout.Acknowledged), ChangeText.Fingerprints))
        {
            MutationIgnore = Documents.Read(Path.Combine(root, Layout.MutationIgnore), text => ChangeText.Patterns(text, "ignore")),
            Waivers = Documents.Read(Path.Combine(root, Layout.Waivers), text => ChangeText.Patterns(text, "waivers")),
            Lock = Documents.Read(Path.Combine(root, Layout.Lock), ChangeText.Lock),
        };
}
