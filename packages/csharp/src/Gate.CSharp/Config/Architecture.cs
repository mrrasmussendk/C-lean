using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gate.CSharp.Model;
using YamlDotNet.RepresentationModel;

namespace Gate.CSharp.Config;

internal sealed record Component(string Name, IReadOnlyList<Regex> Paths, bool Effect);

/// <summary>Reads the components of architecture.yaml: name, path globs and the effect mark.</summary>
internal static class ArchitectureReader
{
    public static IReadOnlyList<Component> Parse(TextFile file) =>
        Yaml.Sequence(file, Yaml.Parse(file), "components").Select(node => Component(file, node)).ToList();

    private static Component Component(TextFile file, YamlNode node)
    {
        var paths = Yaml.Sequence(file, node, "paths").Select(path => Globs.Compile(Yaml.Text(path))).ToList();
        return new Component(Yaml.Required(file, node, "name"), paths, Yaml.Text(Yaml.Child(node, "effect")) == "true");
    }
}
