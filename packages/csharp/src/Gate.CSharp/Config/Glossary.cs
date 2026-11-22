using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Model;
using YamlDotNet.RepresentationModel;

namespace Gate.CSharp.Config;

/// <summary>Reads .gate/glossary.yaml: terms, each with its banned synonyms.</summary>
internal static class GlossaryReader
{
    public const string Path = ".gate/glossary.yaml";

    public static IReadOnlyList<GlossaryTerm> Parse(IReadOnlyList<TextFile> tree) =>
        tree.Where(file => file.Path == Path).SelectMany(Terms).ToList();

    private static IEnumerable<GlossaryTerm> Terms(TextFile file) =>
        Yaml.Sequence(file, Yaml.Parse(file), "terms").Select(node => Term(file, node));

    private static GlossaryTerm Term(TextFile file, YamlNode node)
    {
        var banned = Yaml.Sequence(file, node, "banned").Select(word => Yaml.Text(word).ToLowerInvariant()).ToList();
        return new GlossaryTerm(Yaml.Required(file, node, "term").ToLowerInvariant(), banned);
    }
}
