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

    public static string UserName(string root) =>
        Git(root, ["config", "user.name"]).Trim();

    public static string Git(string root, IReadOnlyList<string> arguments)
    {
        var result = Processes.Run(new ProcessCall("git", arguments, root));
        return result.ExitCode == 0 ? result.Output : throw new GateFailure($"git {string.Join(' ', arguments)} failed: {result.Error.Trim()}");
    }
}
