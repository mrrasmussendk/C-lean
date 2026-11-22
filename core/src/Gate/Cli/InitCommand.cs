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
        var languages = Known.Where(known => tree.Any(path => path.EndsWith(known.Extension))).Select(known => known.Language).ToList();
        var config = Templates.Config.Replace("{languages}", string.Join(", ", languages)).Replace("{packages}", Packages(languages));
        WriteMissing(root, Layout.Config, config);
        WriteMissing(root, ".github/workflows/gate.yml", Templates.Workflow);
        WriteMissing(root, ".github/CODEOWNERS", Templates.Owners);
        WriteMissing(root, ".claude/settings.json", Templates.Hooks);
        WriteMissing(root, ".gate/.gitignore", Templates.Ignored);
        WriteMissing(root, Layout.MutationIgnore, Templates.MutationIgnore);
        MakeStrict(root, tree);
        if (!Files.Exists(Path.Combine(root, Layout.Architecture)))
        {
            DraftArchitecture(root);
        }

        WriteBaseline(root);
        Surroundings.Print("gate: initialised. A human edits architecture.yaml and gate.yaml and accepts .gate/baseline.yaml; all are protected from the implementer.");
        return C6Hardening.Passed;
    }

    /// <summary>The native package of each language, pinned to where it is installed next to this gate and to its version.</summary>
    private static string Packages(IReadOnlyList<string> languages) =>
        string.Join('\n', languages.Select(language => (language, command: Installed(language)))
            .Select(found => found.command.Length == 0
                ? $"  # {found.language}: gate-{found.language} was not found next to the gate; it must be on PATH"
                : $"  {found.language}: {{command: {found.command}, version: {Version(found.command)}}}"));

    private static string Installed(string language) =>
        Surroundings.InstallDirectories()
            .Select(directory => Path.Combine(directory, "packages", language, "bin", $"gate-{language}"))
            .FirstOrDefault(Files.Exists, "");

    private static string Version(string command) =>
        Processes.Run(new ProcessCall(command, ["--version"], Options.Here)).Output.Trim().Split(' ')[0];

    /// <summary>Raises the strict modes the rules depend on. A setting that is already strict is left alone; nothing is lowered.</summary>
    private static void MakeStrict(string root, IReadOnlyList<string> tree)
    {
        foreach (var relative in tree)
        {
            var path = Path.Combine(root, relative);
            var before = Files.Read(path);
            var after = StrictModes.Apply(relative, before);
            if (after != before)
            {
                Files.Write(path, after);
                Surroundings.Print($"made strict: {relative}");
            }
        }
    }

    /// <summary>The existing violations, as a baseline for a human to accept. Written once; a later run keeps it.</summary>
    private static void WriteBaseline(string root)
    {
        var config = InputReader.Config(root);
        if (Files.Exists(Path.Combine(root, Layout.Baseline)) || NativeRunner.HeadFailure(root, config).Length > 0)
        {
            return;
        }

        var findings = SnapshotReader.Read(Path.Combine(root, Layout.HeadOut), config.Languages).Findings;
        var lines = findings.Select(finding => finding.Fingerprint).Distinct().Order().Select(fingerprint => $"  - \"{fingerprint}\"\n");
        Files.Write(Path.Combine(root, Layout.Baseline), $"# Violations that existed when the gate was installed: reported, not enforced. New ones block.\nfingerprints:\n{string.Concat(lines)}");
        Surroundings.Print($"wrote {Layout.Baseline} with {findings.Count} existing violations for a human to accept");
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
