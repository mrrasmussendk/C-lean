using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;

namespace Gate.Cli;

/// <summary>`c-lean init`: idempotent. It writes what is missing and never changes or lowers what is there.</summary>
internal static class InitCommand
{
    private static readonly IReadOnlyList<(string Language, string Extension)> Known = [("csharp", ".cs"), ("typescript", ".ts"), ("typescript", ".tsx"), ("typescript", ".mts"), ("typescript", ".cts"), ("python", ".py")];

    public static int Run(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var tree = Files.Tree(root);
        var languages = Known.Where(known => tree.Any(path => path.EndsWith(known.Extension))).Select(known => known.Language).Distinct().ToList();
        if (languages.Count == 0 && !Files.Exists(Path.Combine(root, Layout.Config)))
        {
            throw new GateFailure($"{Files.Absolute(root)}: no C#, TypeScript or Python source was found here, so there is nothing to gate yet. Run `c-lean init` in the root of your project, or pass --root <dir>. Nothing was written.");
        }

        var config = Templates.Config.Replace("{languages}", string.Join(", ", languages)).Replace("{packages}", Packages(root, languages)).Replace("{rules}", RuleCatalogue.Yaml());
        WriteMissing(root, Layout.Config, config);
        WriteFixedFiles(root);
        MakeStrict(root, tree);
        if (!Files.Exists(Path.Combine(root, Layout.Architecture)))
        {
            DraftArchitecture(root);
        }

        WriteBaseline(root);
        Instruct(arguments);
        Surroundings.Print("c-lean: initialised.");
        var pen = Guide.Pen();
        var banner = Surroundings.Watched() ? Art.Banner(pen, "is set up in this repository.") : [];
        var missing = languages.Where(language => NotInstalled(root, language)).Select(Guide.Missing);
        foreach (var line in banner.Concat(missing).Concat(Guide.AfterInit(root, pen)))
        {
            Surroundings.Print(line);
        }

        return C6Hardening.Passed;
    }

    /// <summary>The files whose text does not depend on the repository: the CI job, the owners, the hooks and the ignore lists.</summary>
    private static void WriteFixedFiles(string root)
    {
        WriteMissing(root, ".github/workflows/gate.yml", Templates.Workflow);
        WriteMissing(root, ".github/CODEOWNERS", Templates.Owners);
        WriteMissing(root, ".claude/settings.json", Templates.Hooks);
        WriteMissing(root, ".opencode/plugins/c-lean.js", Templates.OpenCodePlugin);
        WriteMissing(root, ".gate/.gitignore", Templates.Ignored);
        WriteMissing(root, Layout.MutationIgnore, Templates.MutationIgnore);
        WriteMissing(root, Layout.Ignore, Templates.Ignore);
    }

    /// <summary>`c-lean instructions`: writes the generated section into each agent's instruction file, replacing the one that is there.</summary>
    public static int Instruct(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var architecture = Documents.Read(Path.Combine(root, Layout.Architecture), Parse.ConfigText.Architecture);
        var block = Instructions.Block(InputReader.Config(root), architecture, RuleFiles.Load(root));
        foreach (var file in Layout.AgentInstructions)
        {
            WriteSection(root, file, block);
        }

        return C6Hardening.Passed;
    }

    private static void WriteSection(string root, string file, string block)
    {
        var path = Path.Combine(root, file);
        var before = Files.Read(path);
        var after = Instructions.Merge(before, block);
        if (after != before)
        {
            Files.Write(path, after);
        }

        Surroundings.Print(after == before ? $"kept {file}" : $"wrote the gate's section of {file}");
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
