using YamlDotNet.RepresentationModel;

namespace Gate.Parse;

/// <summary>Reads values out of a YAML document. A missing key reads as empty, never as null.</summary>
internal static class Yaml
{
    public static YamlMappingNode Root(string text)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(text));
        var root = stream.Documents.Count > 0 ? stream.Documents[0].RootNode : new YamlMappingNode();
        return root as YamlMappingNode ?? new YamlMappingNode();
    }

    public static string Text(YamlMappingNode map, string key) =>
        Child(map, key) is YamlScalarNode scalar ? scalar.Value ?? "" : "";

    public static bool Flag(YamlMappingNode map, string key) =>
        Text(map, key) == "true";

    public static int Number(YamlMappingNode map, string key) =>
        int.TryParse(Text(map, key), out var number) ? number : 0;

    public static YamlMappingNode Map(YamlMappingNode map, string key) =>
        Child(map, key) as YamlMappingNode ?? new YamlMappingNode();

    public static IReadOnlyList<YamlMappingNode> Maps(YamlMappingNode map, string key) =>
        Nodes(map, key).OfType<YamlMappingNode>().ToList();

    /// <summary>
    /// A scalar reads as a list of one, so `reason: fix` and `reason: [fix]` are the same. A key with nothing after
    /// it is an empty list, not a list of one empty text.
    /// </summary>
    public static IReadOnlyList<string> Texts(YamlMappingNode map, string key) =>
        Child(map, key) is YamlScalarNode scalar
            ? new[] { scalar.Value ?? "" }.Where(value => value.Length > 0).ToList()
            : Nodes(map, key).OfType<YamlScalarNode>().Select(node => node.Value ?? "").ToList();

    public static IReadOnlyList<string> Keys(YamlMappingNode map) =>
        map.Children.Keys.OfType<YamlScalarNode>().Select(node => node.Value ?? "").ToList();

    private static IReadOnlyList<YamlNode> Nodes(YamlMappingNode map, string key) =>
        Child(map, key) is YamlSequenceNode sequence ? sequence.Children.ToList() : [];

    private static YamlNode? Child(YamlMappingNode map, string key) =>
        map.Children.TryGetValue(new YamlScalarNode(key), out var node) ? node : null;
}
