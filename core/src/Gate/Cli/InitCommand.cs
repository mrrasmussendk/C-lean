using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;

namespace Gate.Cli;

/// <summary>`gate init`: idempotent. It writes what is missing and never changes or lowers what is there.</summary>
internal static class InitCommand
{
    private static readonly IReadOnlyList<(string Language, string Extension)> Known = [("csharp", ".cs"), ("typescript", ".ts"), ("python", ".py")];

    public static int Run(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var tree = Files.Tree(root);
        var languages = Known.Where(known => tree.Any(path => path.EndsWith(known.Extension))).Select(known => known.Language);
        WriteMissing(root, Layout.Config, Templates.Config.Replace("{languages}", string.Join(", ", languages)));
        WriteMissing(root, ".github/workflows/gate.yml", Templates.Workflow);
        WriteMissing(root, ".github/CODEOWNERS", Templates.Owners);
        WriteMissing(root, ".claude/settings.json", Templates.Hooks);
        WriteMissing(root, ".gate/.gitignore", Templates.Ignored);
        if (!Files.Exists(Path.Combine(root, Layout.Architecture)))
        {
            DraftArchitecture(root);
        }

        Surroundings.Print("gate: initialised. A human edits architecture.yaml and gate.yaml; both are protected from the implementer.");
        return C6Hardening.Passed;
    }

    private static void WriteMissing(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative);
        if (Files.Exists(path))
        {
            Surroundings.Print($"kept {relative}");
            return;
        }

        Files.Write(path, text);
        Surroundings.Print($"wrote {relative}");
    }

    /// <summary>Runs the native packages against a one-component model to learn the graph, then proposes a model from it.</summary>
    private static void DraftArchitecture(string root)
    {
        var path = Path.Combine(root, Layout.Architecture);
        Files.Write(path, Templates.ProvisionalArchitecture);
        var config = InputReader.Config(root);
        var failure = NativeRunner.HeadFailure(root, config);
        if (failure.Length > 0)
        {
            Surroundings.Print($"wrote a one-component {Layout.Architecture}: {failure}");
            return;
        }

        var graph = SnapshotReader.Read(Path.Combine(root, Layout.HeadOut), config.Languages).Graph;
        Files.Write(path, ArchitectureDraft.Text(graph));
        Surroundings.Print($"wrote {Layout.Architecture} from the current graph");
    }
}
