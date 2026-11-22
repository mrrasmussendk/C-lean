using Gate.Effects;
using Gate.Model;

namespace Gate.Collect;

internal sealed record Analysis(string Root, string Sources, string Out);

/// <summary>Runs each language's native package on the head and on the base of the change.</summary>
internal static class NativeRunner
{
    public static void AnalyzeHead(string root, Config config) =>
        Analyze(config, new Analysis(root, root, Layout.In(root, Layout.HeadOut)));

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
        foreach (var language in config.Languages)
        {
            var arguments = Arguments(analysis, language);
            var result = Processes.Run(new ProcessCall(Command(analysis.Root, config, language), arguments, analysis.Root));
            if (result.ExitCode != 0)
            {
                throw new GateFailure($"gate-{language} failed with exit code {result.ExitCode}: {result.Error.Trim()}");
            }
        }
    }

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
