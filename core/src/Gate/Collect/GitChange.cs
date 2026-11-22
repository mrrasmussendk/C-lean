using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Asks git what the change did.</summary>
internal static class GitChange
{
    public static string MergeBase(string root, string baseRef) =>
        Git(root, ["merge-base", baseRef, "HEAD"]).Trim();

    public static ChangeSet Read(string root, string mergeBase)
    {
        var format = $"--format={GitText.CommitMark}%H{GitText.FieldMark}%an{GitText.FieldMark}%ae";
        var log = Git(root, ["log", "--reverse", format, "--name-only", $"{mergeBase}..HEAD"]);
        var diff = Git(root, ["diff", "-U0", mergeBase, "HEAD"]);
        return new ChangeSet(GitText.Commits(log), GitText.Lines(diff));
    }

    /// <summary>A throwaway checkout of one revision. The caller removes it with <see cref="RemoveTree"/>.</summary>
    public static string AddTree(string root, string revision)
    {
        var tree = Surroundings.TemporaryDirectory();
        Git(root, ["worktree", "add", "--detach", tree, revision]);
        return tree;
    }

    public static void RemoveTree(string root, string tree) =>
        Git(root, ["worktree", "remove", "--force", tree]);

    /// <summary>First-parent history of the trailing window, oldest first, with the paths each commit changed.</summary>
    public static IReadOnlyList<Commit> Window(string root, string mergeBase)
    {
        var format = $"--format={GitText.CommitMark}%H{GitText.FieldMark}%an{GitText.FieldMark}%ae";
        return GitText.Commits(Git(root, ["log", "--first-parent", "-m", "--reverse", $"--since={WindowDays}.days", format, "--name-only", mergeBase]));
    }

    private const int WindowDays = 90;

    public static string UserName(string root) =>
        Git(root, ["config", "user.name"]).Trim();

    public static string Git(string root, IReadOnlyList<string> arguments)
    {
        var result = Processes.Run(new ProcessCall("git", arguments, root));
        return result.ExitCode == 0 ? result.Output : throw new GateFailure($"`git {string.Join(' ', arguments)}` in {root} failed with exit code {result.ExitCode}: {result.Error.Trim()}. The gate needs a git repository in which the base revision exists; pass --base <ref> or fetch it.");
    }
}
