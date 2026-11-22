using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Config;
using Gate.CSharp.Model;
using YamlDotNet.RepresentationModel;

namespace Gate.CSharp.Mutants;

internal sealed record LineRange(string Path, int Start, int End);

/// <summary>Reads the --lines file: `lines: [{path, start, end}]`, paths relative to the root.</summary>
internal static class LineFilter
{
    public static IReadOnlyList<LineRange> Parse(TextFile file) =>
        Yaml.Sequence(file, Yaml.Parse(file), "lines").Select(node => Range(file, node)).ToList();

    public static bool Holds(IReadOnlyList<LineRange> ranges, Mutant mutant) =>
        ranges.Any(range => range.Path == mutant.File && range.Start <= mutant.Line && mutant.Line <= range.End);

    private static LineRange Range(TextFile file, YamlNode node) => new(
        Yaml.Required(file, node, "path"),
        Yaml.Integer(file, Yaml.Child(node, "start") ?? node, "start"),
        Yaml.Integer(file, Yaml.Child(node, "end") ?? node, "end"));
}
