using Gate.Effects;
using Gate.Model;

namespace Gate.Collect;

internal sealed record Analysis(string Root, string Sources, string Out);

/// <summary>Runs each language's native package on the head and on the base of the change.</summary>
internal static class NativeRunner
{
    public static void AnalyzeHead(string root, Config config) =>
        Analyze(config, new Analysis(root, root, Layout.In(root, Layout.HeadOut)));

    /// <summary>What stops the head from being analyzed, or empty when every package ran.</summary>
    public static string HeadFailure(string root, Config config) =>
        Failures(config, new Analysis(root, root, Layout.In(root, Layout.HeadOut))).FirstOrDefault("");

    public static void AnalyzeBase(string root, Config config, string mergeBase)
    {
        var tree = Surroundings.TemporaryDirectory();
        GitChange.Git(root, ["worktree", "add", "--detach", tree, mergeBase]);
        try
        {
            Analyze(config, new Analysis(root, tree, Layout.In(root, Layout.BaseOut)));
        }
        finally
        {
            GitChange.Git(root, ["worktree", "remove", "--force", tree]);
        }
    }

    public static string Command(string root, Config config, string language)
    {
        var configured = config.Commands.GetValueOrDefault(language, "");
        return configured.Length == 0 ? $"gate-{language}"
            : configured.Contains('/') ? Path.GetFullPath(Layout.In(root, configured))
            : configured;
    }

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
        select $"`{call.Command} analyze --root {analysis.Sources}` failed with exit code {result.ExitCode}:\n      {result.Error.Trim().ReplaceLineEndings("\n      ")}\n      The {language} package must be installed and run cleanly; `gate doctor` shows what is installed.";

    /// <summary>The head's protected files configure both runs, so base and head are measured alike.</summary>
    private static IReadOnlyList<string> Arguments(Analysis analysis, string language) =>
    [
        "analyze",
        "--root", Path.GetFullPath(analysis.Sources),
        "--out", Path.GetFullPath(Path.Combine(analysis.Out, language)),
        "--config", Path.GetFullPath(Layout.In(analysis.Root, Layout.Config)),
        "--architecture", Path.GetFullPath(Layout.In(analysis.Root, Layout.Architecture)),
        "--today", Surroundings.Today(),
    ];
}
