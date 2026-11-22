using System;
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;
using Gate.CSharp.Mutants;
using Gate.CSharp.Output;

namespace Gate.CSharp.Pipeline;

/// <summary>
/// `mutants`: every mutant of production code, or only those on the lines of the --lines file.
/// With --switch the listed mutants are also compiled into the sources (see MutantSwitcher).
/// </summary>
internal static class MutantLister
{
    private const string BackedOut = "CompileError";

    public static Report List(Inputs inputs, ReferenceSet references)
    {
        var solution = Workspace.Load(inputs, references).Solution;
        var sites = Sites(solution, inputs);
        var outcome = inputs.Options.Switch
            ? MutantSwitcher.Switch(inputs, references, sites)
            : new MutantSwitcher.Outcome(sites.Select(site => site.Mutant.Id).ToHashSet(), []);
        var mutants = sites.Select(site => site.Mutant)
            .Select(mutant => outcome.Switched.Contains(mutant.Id) ? mutant : mutant with { Status = BackedOut }).ToList();
        var listing = new TextFile("mutants.json", MutantsJson.Render(new MutantList(mutants, inputs.Options.Switch)));
        var sources = outcome.Files.Select(file => file with { Path = $"{inputs.RootPath}/{file.Path}" });
        return new Report(inputs.Options.Out, sources.Prepend(listing).ToList(), solution.Problems);
    }

    private static IReadOnlyList<Site> Sites(Solution solution, Inputs inputs)
    {
        var marks = inputs.Tree.ToDictionary(file => file.Path, file => file.ByteOrderMark);
        var ranges = LineFilter.Parse(inputs.Lines);
        return solution.Files.Where(file => !file.IsTest)
            .SelectMany(file => MutantFinder.Of(file, marks.GetValueOrDefault(file.Path)))
            .Where(site => inputs.Options.Lines == "" || LineFilter.Holds(ranges, site.Mutant))
            .GroupBy(site => site.Mutant.Id)
            .SelectMany(same => same.Select((site, index) => same.Count() == 1 ? site : site with { Mutant = site.Mutant with { Id = $"{site.Mutant.Id}#{index + 1}" } }))
            .OrderBy(site => site.Mutant.File, StringComparer.Ordinal).ThenBy(site => site.Mutant.Start)
            .ThenBy(site => site.Mutant.Mutator, StringComparer.Ordinal).ToList();
    }
}
