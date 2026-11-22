using System;
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;
using Gate.CSharp.Mutants;
using Gate.CSharp.Output;

namespace Gate.CSharp.Pipeline;

/// <summary>`mutants`: every mutant of production code, or only those on the lines of the --lines file.</summary>
internal static class MutantLister
{
    public static Report List(Inputs inputs, ReferenceSet references)
    {
        var solution = Workspace.Load(inputs, references).Solution;
        var marks = inputs.Tree.ToDictionary(file => file.Path, file => file.ByteOrderMark);
        var ranges = LineFilter.Parse(inputs.Lines);
        var mutants = solution.Files.Where(file => !file.IsTest)
            .SelectMany(file => MutantFinder.Of(file, marks.GetValueOrDefault(file.Path)))
            .Where(mutant => inputs.Options.Lines == "" || LineFilter.Holds(ranges, mutant))
            .GroupBy(mutant => mutant.Id)
            .SelectMany(same => same.Select((mutant, index) => same.Count() == 1 ? mutant : mutant with { Id = $"{mutant.Id}#{index + 1}" }))
            .OrderBy(mutant => mutant.File, StringComparer.Ordinal).ThenBy(mutant => mutant.Start)
            .ThenBy(mutant => mutant.Mutator, StringComparer.Ordinal).ToList();
        return new Report(inputs.Options.Out, [new TextFile("mutants.json", MutantsJson.Render(mutants))], solution.Problems);
    }
}
