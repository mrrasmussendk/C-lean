using System.Collections.Generic;
using Gate.CSharp.Config;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;

namespace Gate.CSharp.Pipeline;

internal sealed record Loaded(Solution Solution, Settings Settings, IReadOnlyList<ProjectSpec> Projects);

/// <summary>What every command starts from: the config read, the projects found and compiled.</summary>
internal static class Workspace
{
    public static Loaded Load(Inputs inputs, ReferenceSet references)
    {
        var config = GateConfigReader.Parse(inputs.Config, inputs.Ignore);
        var classification = new Classification(config, ArchitectureReader.Parse(inputs.Architecture));
        var settings = RuleTable.Settings(config, inputs.Options.Today, GlossaryReader.Parse(inputs.Tree));
        var projects = ProjectLayout.Of(inputs.Tree, inputs.RootName);
        return new Loaded(Compiler.Build(projects, references, classification), settings, projects);
    }
}
