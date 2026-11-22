using Gate.CSharp.Config;
using Gate.CSharp.Graph;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;
using Gate.CSharp.Output;

namespace Gate.CSharp.Pipeline;

/// <summary>The whole analysis as a function of what was read: texts and references in, the three reports out.</summary>
internal static class Analyzer
{
    public static Report Analyze(Inputs inputs, ReferenceSet references)
    {
        var config = GateConfigReader.Parse(inputs.Config);
        var classification = new Classification(config, ArchitectureReader.Parse(inputs.Architecture));
        var settings = RuleTable.Settings(config, inputs.Options.Today, GlossaryReader.Parse(inputs.Tree));
        var solution = Compiler.Build(ProjectLayout.Of(inputs.Tree, inputs.RootName), references, classification);
        var graph = GraphBuilder.Build(solution);
        return new Report(
            Sarif.Render(RuleRunner.Run(solution, settings), settings),
            GraphJson.Render(graph),
            UnitsJson.Render(UnitsBuilder.Build(solution, graph, settings)),
            solution.Problems);
    }
}
