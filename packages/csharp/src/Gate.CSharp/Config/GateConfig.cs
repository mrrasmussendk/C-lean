using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gate.CSharp.Model;
using YamlDotNet.RepresentationModel;

namespace Gate.CSharp.Config;

internal sealed record GateConfig(
    IReadOnlyList<Regex> Tests,
    IReadOnlyList<Regex> Generated,
    IReadOnlyDictionary<string, int> Limits,
    IReadOnlyDictionary<string, string> Severities);

/// <summary>Reads gate.yaml: paths.tests, paths.generated, rules.ID.limit and rules.ID.severity.</summary>
internal static class GateConfigReader
{
    private static readonly string[] Severities = ["block", "warn", "report"];

    public static GateConfig Parse(TextFile file)
    {
        var root = Yaml.Parse(file);
        var paths = Yaml.Mapping(file, root, "paths");
        var rules = Yaml.Entries(Yaml.Mapping(file, root, "rules")).ToList();
        return new GateConfig(
            Patterns(file, paths, "tests"),
            Patterns(file, paths, "generated"),
            Setting(rules, "limit").ToDictionary(pair => pair.Key, pair => Yaml.Integer(file, pair.Value, $"rules.{pair.Key}.limit")),
            Setting(rules, "severity").ToDictionary(pair => pair.Key, pair => Severity(file, pair.Value, pair.Key)));
    }

    private static IReadOnlyList<Regex> Patterns(TextFile file, YamlNode paths, string key) =>
        Yaml.Sequence(file, paths, key).Select(node => Globs.Compile(Yaml.Text(node))).ToList();

    private static IEnumerable<KeyValuePair<string, YamlNode>> Setting(
        IReadOnlyList<KeyValuePair<YamlNode, YamlNode>> rules,
        string key) =>
        rules.Select(rule => (Id: Yaml.Text(rule.Key), Value: Yaml.Child(rule.Value, key)))
            .Where(rule => rule.Value is not null)
            .Select(rule => new KeyValuePair<string, YamlNode>(rule.Id, rule.Value ?? new YamlScalarNode("")));

    private static string Severity(TextFile file, YamlNode node, string rule) =>
        Severities.Contains(Yaml.Text(node))
            ? Yaml.Text(node)
            : throw Yaml.Wrong(file, node, $"key 'rules.{rule}.severity'", "block, warn or report");
}
