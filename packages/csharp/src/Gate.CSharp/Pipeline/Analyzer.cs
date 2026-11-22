using Gate.CSharp.Graph;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;
using Gate.CSharp.Output;

namespace Gate.CSharp.Pipeline;

/// <summary>`analyze` as a function of what was read: texts and references in, the three interchange files
/// out.</summary>
internal static class Analyzer
{
    public static Report Analyze(Inputs inputs, ReferenceSet references)
    {
        var (solution, settings, _) = Workspace.Load(inputs, references);
        var graph = GraphBuilder.Build(solution);
        return new Report(
            inputs.Options.Out,
            [
                new TextFile("findings.sarif", Sarif.Render(RuleRunner.Run(solution, settings), settings)),
                new TextFile("graph.json", GraphJson.Render(graph)),
                new TextFile("units.json", UnitsJson.Render(UnitsBuilder.Build(solution, graph, settings))),
            ],
            solution.Problems);
    }
}
