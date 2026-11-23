using System.Text.Json;
using System.Xml;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Gate.Parse;

/// <summary>Says where and why a file the gate reads is wrong, in words a person can act on.</summary>
internal static class Malformed
{
    private static readonly IReadOnlyList<string> Languages = ["csharp", "typescript", "python"];

    public static bool IsSyntax(Exception problem) =>
        problem is YamlException or JsonException or XmlException;

    /// <summary>`line:column: reason` for a file that does not parse.</summary>
    public static string Syntax(Exception problem) => problem switch
    {
        YamlException yaml => $"{yaml.Start.Line}:{yaml.Start.Column}: not valid YAML: {Reason((yaml.InnerException ?? yaml).Message)}",
        JsonException json => $"{json.LineNumber + 1}:{json.BytePositionInLine + 1}: not valid JSON: {Reason(json.Message)}",
        XmlException xml => $"{xml.LineNumber}:{xml.LinePosition}: not valid XML: {Reason(xml.Message)}",
        _ => problem.Message,
    };

    /// <summary>The parser's reason without the position it repeats and without a closing full stop.</summary>
    private static string Reason(string message) =>
        message.Split(" LineNumber:")[0].Split(" Line ")[0].TrimEnd(' ', '.');

    /// <summary>What is wrong with a gate.yaml that parses, each as `line: reason`.</summary>
    public static IReadOnlyList<string> Config(string text)
    {
        var root = Yaml.Root(text);
        var languages = Yaml.Texts(root, "languages");
        var unknown = languages.Where(language => !Languages.Contains(language))
            .Select(language => $"{Line(root, "languages")}: `languages` names '{language}'. Known languages are {string.Join(", ", Languages)}.");
        var missing = new[] { "spec", "languages", "implementers", "protected" }
            .Where(key => Yaml.Texts(root, key).Count == 0)
            .Select(key => $"1: `{key}` is missing or empty. Add it; `c-lean init` writes a complete example.");
        return [.. missing, .. unknown];
    }

    /// <summary>What is wrong with an architecture.yaml that parses, each as `line: reason`.</summary>
    public static IReadOnlyList<string> Architecture(string text)
    {
        var root = Yaml.Root(text);
        var components = Yaml.Maps(root, "components");
        var names = components.Select(component => Yaml.Text(component, "name")).ToHashSet();
        var unnamed = components.Where(component => Yaml.Text(component, "name").Length == 0 || Yaml.Texts(component, "paths").Count == 0)
            .Select(component => $"{component.Start.Line}: a component needs a `name` and at least one glob in `paths`.");
        var dangling = Yaml.Maps(root, "edges")
            .SelectMany(edge => new[] { "from", "to" }.Select(end => (edge, end, name: Yaml.Text(edge, end))))
            .Where(use => !names.Contains(use.name))
            .Select(use => $"{use.edge.Start.Line}: edge `{use.end}: {use.name}` names a component that is not declared under `components`.");
        var empty = components.Count == 0 ? ["1: `components` is missing or empty. Declare at least one component."] : Array.Empty<string>();
        return [.. empty, .. unnamed, .. dangling];
    }

    private static long Line(YamlMappingNode root, string key) =>
        root.Children.Where(pair => pair.Key is YamlScalarNode scalar && scalar.Value == key).Select(pair => pair.Key.Start.Line).FirstOrDefault(1);
}
