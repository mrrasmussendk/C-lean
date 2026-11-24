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
        var log = Git(root, [.. Plain, "log", "-z", "--reverse", "--no-renames", "-c", format, "--name-only", $"{mergeBase}..HEAD"]);
        var diff = Git(root, [.. Plain, "diff", "--no-color", "--no-ext-diff", "--no-renames", "--src-prefix=a/", "--dst-prefix=b/", "-U0", mergeBase, "HEAD"]);
        return new ChangeSet(GitText.Commits(log), GitText.Lines(diff));
    }

    /// <summary>
    /// Settings that make git's output the same on every machine, whatever the user's configuration: paths are
    /// printed as they are, not quoted and escaped, and the log ends each path with a NUL (`-z`), so that a name
    /// with a quote, a tab or a backslash in it arrives unchanged. With them go, per command: no rename detection, so a file that
    /// is moved shows under both its old and its new name; `-c` on the log, so a merge commit lists the paths it
    /// changed itself; and fixed prefixes without colour on the diff.
    /// </summary>
    private static readonly IReadOnlyList<string> Plain = ["-c", "core.quotepath=off"];

    /// <summary>
    /// A checkout of one revision in a directory kept between runs, reset to exactly that revision.
    /// Files git ignores (build outputs, restored packages) survive, so the next build is incremental.
    /// </summary>
    public static string Tree(string root, string name, string revision)
    {
        var tree = Surroundings.TreeDirectory(root, name);
        var commit = Git(root, ["rev-parse", "--verify", $"{revision}^{{commit}}"]).Trim();
        Git(root, ["worktree", "prune"]);
        if (Files.Exists(Path.Combine(tree, ".git")) && Processes.Run(new ProcessCall("git", ["rev-parse", "--git-dir"], tree)).ExitCode != 0)
        {
            Files.RemoveDirectory(tree);
            Git(root, ["worktree", "prune"]);
        }

        if (Files.Exists(Path.Combine(tree, ".git")))
        {
            Git(tree, ["checkout", "--quiet", "--force", "--detach", commit]);
            Git(tree, ["clean", "--quiet", "-fd"]);
            return tree;
        }

        Git(root, ["worktree", "add", "--force", "--detach", tree, commit]);
        return tree;
    }

    /// <summary>First-parent history of the trailing window, oldest first, with the paths each commit changed.</summary>
    public static IReadOnlyList<Commit> Window(string root, string mergeBase)
    {
        var format = $"--format={GitText.CommitMark}%H{GitText.FieldMark}%an{GitText.FieldMark}%ae";
        return GitText.Commits(Git(root, [.. Plain, "log", "-z", "--no-renames", "--first-parent", "-m", "--reverse", $"--since={WindowDays}.days", format, "--name-only", mergeBase]));
    }

    private const int WindowDays = 90;

    /// <summary>The branch being worked on, which names the change in the gate's log; empty outside a repository.</summary>
    public static string Branch(string root)
    {
        var result = Processes.Run(new ProcessCall("git", ["rev-parse", "--abbrev-ref", "HEAD"], root));
        return result.ExitCode == 0 ? result.Output.Trim() : "";
    }

    public static string UserName(string root) =>
        Git(root, ["config", "user.name"]).Trim();

    public static string Git(string root, IReadOnlyList<string> arguments)
    {
        var result = Processes.Run(new ProcessCall("git", arguments, root));
        return result.ExitCode == 0 ? result.Output : throw new GateFailure($"`git {string.Join(' ', arguments)}` in {root} failed with exit code {result.ExitCode}: {result.Error.Trim()}. The gate needs a git repository in which the base revision exists; pass --base <ref> or fetch it.");
    }
}
