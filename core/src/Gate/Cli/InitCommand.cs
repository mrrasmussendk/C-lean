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
        if (languages.Count == 0 && !Files.Exists(Path.Combine(root, Layout.Config)))
        {
            throw new GateFailure($"{Files.Absolute(root)}: no C#, TypeScript or Python source was found here, so there is nothing to gate yet. Run `gate init` in the root of your project, or pass --root <dir>. Nothing was written.");
        }

        var config = Templates.Config.Replace("{languages}", string.Join(", ", languages)).Replace("{packages}", Packages(root, languages));
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
        Instruct(arguments);
        Surroundings.Print("gate: initialised.");
        foreach (var line in Guide.Obstacles(root, languages.Where(language => NotInstalled(root, language)).ToList()).Concat(Guide.AfterInit(languages)))
        {
            Surroundings.Print(line);
        }

        return C6Hardening.Passed;
    }

    /// <summary>`gate instructions`: writes the generated section into the agent's instruction file, replacing the one that is there.</summary>
    public static int Instruct(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var architecture = Documents.Read(Path.Combine(root, Layout.Architecture), Parse.ConfigText.Architecture);
        var path = Path.Combine(root, Layout.AgentInstructions);
        var before = Files.Read(path);
        var after = Instructions.Merge(before, Instructions.Block(InputReader.Config(root), architecture));
        if (after != before)
        {
            Files.Write(path, after);
        }

        Surroundings.Print(after == before ? $"kept {Layout.AgentInstructions}" : $"wrote the gate's section of {Layout.AgentInstructions}");
        return C6Hardening.Passed;
    }

    /// <summary>
    /// The native package of each language, pinned to its version. No path is written: gate.yaml is shared, and each
    /// machine finds the package next to its own gate.
    /// </summary>
    private static string Packages(string root, IReadOnlyList<string> languages) =>
        string.Join('\n', languages.Select(language => (language, version: Version(NativeRunner.Command(root, InputReader.Unpinned, language))))
            .Select(found => found.version.Length == 0
                ? $"  # {found.language}: gate-{found.language} is not installed here; run the installer"
                : $"  {found.language}: {{version: {found.version}}}"));

    private static bool NotInstalled(string root, string language) =>
        Version(NativeRunner.Command(root, InputReader.Config(root), language)).Length == 0;

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
