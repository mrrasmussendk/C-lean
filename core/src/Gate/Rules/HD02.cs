// HD02: A change by the implementer must not touch a protected path
using Gate.Model;

namespace Gate.Rules;

internal static class HD02
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var config = input.Config;
        var touched = input.Change.Commits
            .Where(commit => ByImplementer(config, commit))
            .SelectMany(commit => commit.Paths.Select(path => (commit, path)));
        return touched
            .Where(touch => Glob.MatchesAny(config.Protected, touch.path))
            .Select(touch => Finding(touch.commit, touch.path))
            .ToList();
    }

    public static bool ByImplementer(Config config, Commit commit) =>
        config.Implementers.Any(implementer => commit.Author.Contains(implementer));

    private static Finding Finding(Commit commit, string path)
    {
        var message = $"HD02: commit {commit.Id} by implementer '{commit.Author}' changes {path}, which `protected` in gate.yaml reserves for humans. Revert that file in the change. If the task cannot be done without it, run `gate conflict`.";
        return new Finding("HD02", Severities.Block, new Location(path, 0), message)
        {
            Fingerprint = $"HD02|{path}|{commit.Id}",
        };
    }
}
