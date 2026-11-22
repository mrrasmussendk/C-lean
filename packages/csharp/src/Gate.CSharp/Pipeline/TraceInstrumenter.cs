using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;
using Gate.CSharp.Tracing;

namespace Gate.CSharp.Pipeline;

/// <summary>`instrument`: the rewritten production files and one helper file per project, to be written over the root.</summary>
internal static class TraceInstrumenter
{
    public static Report Instrument(Inputs inputs, ReferenceSet references)
    {
        var (solution, _, projects) = Workspace.Load(inputs, references);
        var marks = inputs.Tree.ToDictionary(file => file.Path, file => file.ByteOrderMark);
        var rewritten = solution.Files.Where(file => !file.IsTest)
            .Select(file => new TextFile(file.Path, Instrumenter.Rewrite(file, TraceHelper.ClassName(file.Assembly)), marks.GetValueOrDefault(file.Path)));
        var helpers = projects.Select(project => new TextFile(
            Paths.Directory(project.Path) + TraceHelper.FileName,
            TraceHelper.Source(project.Name, inputs.Options.TraceFile)));
        return new Report(inputs.Options.Root, rewritten.Concat(helpers).ToList(), solution.Problems);
    }
}
