using Gate.Effects;
using Gate.Model;

namespace Gate.Collect;

internal sealed record Analysis(string Root, string Sources, string Out);

/// <summary>Runs each language's native package on the head and on the base of the change.</summary>
internal static class NativeRunner
{
    public static void AnalyzeHead(string root, Config config) =>
        Analyze(config, new Analysis(root, root, Path.Combine(root, Layout.HeadOut)));

    /// <summary>What stops the head from being analyzed, or empty when every package ran.</summary>
    public static string HeadFailure(string root, Config config) =>
        Failures(config, new Analysis(root, root, Path.Combine(root, Layout.HeadOut))).FirstOrDefault("");

    public static void AnalyzeBase(string root, Config config, string tree) =>
        Analyze(config, new Analysis(root, tree, Path.Combine(root, Layout.BaseOut)));

    /// <summary>Runs one more command of a language's package (`mutants`, `instrument`) and fails the gate if it fails.</summary>
    public static void Run(Analysis analysis, string language, IReadOnlyList<string> arguments)
    {
        var command = Command(analysis.Root, InputReader.Config(analysis.Root), language);
        var result = Processes.Run(new ProcessCall(command, arguments, analysis.Root));
        if (result.ExitCode != 0)
        {
            throw new GateFailure($"`{command} {string.Join(' ', arguments)}` failed with exit code {result.ExitCode}:\n      {result.Error.Trim().ReplaceLineEndings("\n      ")}");
        }
    }

    /// <summary>Analyzes the working tree with one language's package only, for the check that follows an edit.</summary>
    public static void AnalyzeEdit(string root, Config config, string language)
    {
        var analysis = new Analysis(root, root, Path.Combine(root, Layout.EditOut));
        Run(analysis, language, Arguments(analysis, language));
    }

    /// <summary>
    /// The command of a language's package: the one gate.yaml names, else the package installed next to this gate,
    /// else `gate-&lt;language&gt;` on the PATH. A gate.yaml without a command therefore works on every machine.
    /// </summary>
    public static string Command(string root, Config config, string language)
    {
        var configured = config.Commands.GetValueOrDefault(language, "");
        return configured.Length == 0 ? Installed(language)
            : configured.Contains('/') ? Files.Absolute(Path.Combine(root, configured))
            : configured;
    }

    private static string Installed(string language) =>
        Surroundings.InstallDirectories()
            .Select(directory => Path.Combine(directory, "packages", language, "bin", $"gate-{language}"))
            .FirstOrDefault(Files.Exists, $"gate-{language}");

    private static void Analyze(Config config, Analysis analysis)
    {
        foreach (var failure in Failures(config, analysis))
        {
            throw new GateFailure(failure);
        }
    }

    private static IEnumerable<string> Failures(Config config, Analysis analysis) =>
        from language in config.Languages
        let call = new ProcessCall(Command(analysis.Root, config, language), Arguments(analysis, language), analysis.Root)
        let result = Processes.Run(call)
        where result.ExitCode != 0
        select $"`{call.Command} analyze --root {analysis.Sources}` failed with exit code {result.ExitCode}:\n      {result.Error.Trim().ReplaceLineEndings("\n      ")}\n      The {language} package must be installed and run cleanly; `c-lean doctor` shows what is installed.";

    /// <summary>The head's protected files configure both runs, so base and head are measured alike.</summary>
    private static IReadOnlyList<string> Arguments(Analysis analysis, string language) =>
    [
        "analyze",
        "--root", Files.Absolute(analysis.Sources),
        "--out", Files.Absolute(Path.Combine(analysis.Out, language)),
        "--config", Files.Absolute(Path.Combine(analysis.Root, Layout.Config)),
        "--architecture", Files.Absolute(Path.Combine(analysis.Root, Layout.Architecture)),
        "--today", Surroundings.Today(),
    ];
}
