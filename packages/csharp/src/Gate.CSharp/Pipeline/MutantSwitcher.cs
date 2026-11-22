using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;
using Gate.CSharp.Mutants;

namespace Gate.CSharp.Pipeline;

/// <summary>
/// `mutants --switch`: the sources with every mutant that compiles behind a switch, and the helper
/// of each project. The switched tree is compiled; mutants that break it are backed out and the
/// rest compiled again, until the tree has no error or warning the original did not have.
/// </summary>
internal static class MutantSwitcher
{
    private const int MaxRounds = 8;

    /// <summary>What stays the same over the rounds: what was read, the references, the projects and the original
    /// problems.</summary>
    private sealed record Attempt(
        Inputs Inputs,
        ReferenceSet References,
        IReadOnlyList<ProjectSpec> Projects,
        IReadOnlySet<string> Known);

    /// <summary>The ids of the mutants that are switched in, and the files to write (paths relative to the
    /// root).</summary>
    public sealed record Outcome(IReadOnlySet<string> Switched, IReadOnlyList<TextFile> Files);

    public static Outcome Switch(Inputs inputs, ReferenceSet references, Loaded loaded, IReadOnlyList<Site> sites)
    {
        var attempt = new Attempt(inputs, references, loaded.Projects, SwitchCheck.Problems(loaded.Solution));
        var kept = Settle(attempt, sites.Where(Switcher.CanSwitch).ToList(), 0);
        return new Outcome(kept.Select(site => site.Mutant.Id).ToHashSet(), Files(attempt, kept));
    }

    private static IReadOnlyList<Site> Settle(Attempt attempt, IReadOnlyList<Site> kept, int round)
    {
        var tree = attempt.Inputs.Tree.ExceptBy(Files(attempt, kept).Select(file => file.Path), file => file.Path)
            .Concat(Files(attempt, kept))
            .ToList();
        var switched = Workspace.Load(attempt.Inputs with { Tree = tree }, attempt.References).Solution;
        var offenders = SwitchCheck.Offenders(switched, attempt.Known).ToHashSet();
        if (offenders.Count == 0 || kept.Count == 0)
        {
            return kept;
        }
        if (round == MaxRounds || offenders.Contains(SwitchCheck.Everything))
        {
            return Settle(attempt, [], round);
        }
        var blamed = Switcher.Number(kept)
            .Where(place => offenders.Contains(place.Key))
            .Select(place => place.Site)
            .ToHashSet();
        return Settle(attempt, kept.Where(site => !blamed.Contains(site)).ToList(), round + 1);
    }

    private static IReadOnlyList<TextFile> Files(Attempt attempt, IReadOnlyList<Site> kept)
    {
        var marks = attempt.Inputs.Tree.ToDictionary(file => file.Path, file => file.ByteOrderMark);
        var placed = Switcher.Number(kept);
        var sources = placed.GroupBy(place => place.Site.File)
            .Select(
                same =>
                    new TextFile(
                        same.Key.Path,
                        Switcher.Rewrite(same.Key, same),
                        marks.GetValueOrDefault(same.Key.Path)));
        var helpers = attempt.Projects.Select(
            project =>
                new TextFile(
                    Paths.Directory(project.Path) + Switcher.HelperFile,
                    Switcher.HelperSource(project.Name, placed)));
        return sources.Concat(helpers).ToList();
    }
}
