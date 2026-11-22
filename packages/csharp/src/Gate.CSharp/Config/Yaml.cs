using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Gate.CSharp.Config;

/// <summary>Reads YAML text into nodes and fails with file, line and key when the shape is wrong.</summary>
internal static class Yaml
{
    public static YamlNode Parse(TextFile file)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(file.Text));
        }
        catch (YamlException error)
        {
            throw Failures.At(
                Mark(file, error.Start),
                $"malformed YAML: {error.Message.TrimEnd('.')}",
                "Fix the syntax at this position");
        }
        var root = stream.Documents.Select(document => document.RootNode).FirstOrDefault(new YamlMappingNode());
        return root is YamlMappingNode ? root : throw Wrong(file, root, "the document", "a mapping");
    }

    public static YamlNode? Child(YamlNode? node, string key) =>
        Entries(node).Where(entry => Text(entry.Key) == key).Select(entry => entry.Value).FirstOrDefault();

    public static IEnumerable<KeyValuePair<YamlNode, YamlNode>> Entries(YamlNode? node) =>
        node is YamlMappingNode map ? map.Children : [];

    public static string Text(YamlNode? node) => node is YamlScalarNode scalar ? scalar.Value ?? "" : "";

    public static IReadOnlyList<YamlNode> Sequence(TextFile file, YamlNode? parent, string key)
    {
        var child = Child(parent, key);
        return child switch
        {
            null => [],
            YamlSequenceNode sequence => sequence.Children.ToList(),
            _ => throw Wrong(file, child, $"key '{key}'", "a list"),
        };
    }

    public static YamlNode Mapping(TextFile file, YamlNode? parent, string key)
    {
        var child = Child(parent, key) ?? new YamlMappingNode();
        return child is YamlMappingNode ? child : throw Wrong(file, child, $"key '{key}'", "a mapping");
    }

    public static int Integer(TextFile file, YamlNode node, string key) =>
        int.TryParse(Text(node), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw Wrong(file, node, $"key '{key}'", "a whole number");

    public static string Required(TextFile file, YamlNode parent, string key)
    {
        var text = Text(Child(parent, key));
        return text != "" ? text : throw Wrong(file, parent, $"key '{key}'", "present and a text value");
    }

    public static GateFailure Wrong(TextFile file, YamlNode node, string subject, string expected) =>
        Failures.At(Mark(file, node.Start), $"{subject} must be {expected}", $"Correct it in {file.Path}");

    private static Position Mark(TextFile file, Mark mark) => new(file.Path, (int)mark.Line, (int)mark.Column);
}
