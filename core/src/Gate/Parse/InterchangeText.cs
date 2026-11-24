using System.Text.Json;
using System.Xml.Linq;
using Gate.Model;

namespace Gate.Parse;

/// <summary>The six interchange files a native package writes. An empty text is a file that was not written.</summary>
internal static class InterchangeText
{
    public static IReadOnlyList<Finding> Findings(string sarif) =>
        Json.Items(Root(sarif), "runs").SelectMany(run => Json.Items(run, "results"))
            .Where(result => Json.Items(result, "suppressions").Count == 0)
            .Select(Finding).ToList();

    /// <summary>Whether the text is a JSON object that holds the named list.</summary>
    public static bool Holds(string json, string list) =>
        json.Length > 0 && Root(json) is { ValueKind: JsonValueKind.Object } root && root.TryGetProperty(list, out var value) && value.ValueKind == JsonValueKind.Array;

    public static Graph Graph(string json)
    {
        var root = Root(json);
        return new Graph(Json.Items(root, "nodes").Select(Node).ToList(), Json.Items(root, "edges").Select(Edge).ToList());
    }

    public static IReadOnlyList<Unit> Units(string json) =>
        Json.Items(Root(json), "units").Select(Unit).ToList();

    public static IReadOnlyList<Span> Spans(string otlp) =>
        Json.Items(Root(otlp), "resourceSpans")
            .SelectMany(resource => Json.Items(resource, "scopeSpans"))
            .SelectMany(scope => Json.Items(scope, "spans"))
            .Select(Span).ToList();

    public static IReadOnlyList<TestRun> Tests(string junit) =>
        junit.Length == 0 ? [] : Results(XDocument.Parse(junit));

    /// <summary>JUnit XML, or the TRX that `dotnet test --logger trx` writes without any extra package.</summary>
    private static IReadOnlyList<TestRun> Results(XDocument document)
    {
        var junit = document.Descendants("testcase").Select(TestRun);
        var trx = document.Descendants().Where(element => element.Name.LocalName == "UnitTestResult").Select(TrxRun);
        return junit.Concat(trx).ToList();
    }

    private static TestRun TrxRun(XElement result)
    {
        var seconds = TimeSpan.TryParse(result.Attribute("duration")?.Value, out var duration) ? duration.TotalSeconds : 0;
        return new TestRun(result.Attribute("testName")?.Value ?? "", seconds)
        {
            Failed = result.Attribute("outcome")?.Value is "Failed" or "Error" or "Timeout" or "Aborted",
        };
    }

    /// <summary>The document's root, or an empty element for a file that was not written.</summary>
    private static JsonElement Root(string json)
    {
        if (json.Length == 0)
        {
            return default;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static Finding Finding(JsonElement result)
    {
        var physical = Json.Object(Json.Items(result, "locations").FirstOrDefault(), "physicalLocation");
        var file = Json.Text(Json.Object(physical, "artifactLocation"), "uri");
        var line = Json.Number(Json.Object(physical, "region"), "startLine");
        var message = Json.Text(Json.Object(result, "message"), "text");
        return new Finding(Json.Text(result, "ruleId"), Severities.Named(Json.Text(result, "level")), new Location(file, line), message)
        {
            Fingerprint = Json.Text(Json.Object(result, "partialFingerprints"), "gate/v1"),
        };
    }

    private static Node Node(JsonElement node) =>
        new(Json.Text(node, "id"), Json.Text(node, "kind"), Json.Text(node, "component"), Json.Text(node, "file"))
        {
            Test = Json.Flag(node, "test"),
            Effect = Json.Flag(node, "effect"),
            Property = Json.Flag(node, "property"),
            Line = Json.Number(node, "line"),
        };

    private static Edge Edge(JsonElement edge) =>
        new(Json.Text(edge, "from"), Json.Text(edge, "to"), Json.Text(edge, "kind"));

    private static Unit Unit(JsonElement unit) =>
        new(Json.Text(unit, "id"), Json.Text(unit, "file"), Json.Text(unit, "hash"), Json.Items(unit, "sequences").Select(Sequence).ToList())
        {
            StartLine = Json.Number(unit, "startLine"),
            EndLine = Json.Number(unit, "endLine"),
            Test = Json.Flag(unit, "test"),
            Callers = Json.Number(unit, "callers"),
        };

    private static Sequence Sequence(JsonElement sequence) =>
        new(Json.Text(sequence, "hash"), Json.Number(sequence, "startLine"), Json.Number(sequence, "endLine"));

    private static Span Span(JsonElement span)
    {
        var component = Json.Items(span, "attributes")
            .Where(attribute => Json.Text(attribute, "key") == "gate.component")
            .Select(attribute => Json.Text(Json.Object(attribute, "value"), "stringValue"))
            .FirstOrDefault("");
        return new Span(Json.Text(span, "spanId"), Json.Text(span, "parentSpanId"), component);
    }

    private static TestRun TestRun(XElement testcase)
    {
        var name = $"{testcase.Attribute("classname")?.Value}.{testcase.Attribute("name")?.Value}".TrimStart('.');
        var seconds = double.TryParse(testcase.Attribute("time")?.Value, out var time) ? time : 0;
        return new TestRun(name, seconds)
        {
            Failed = testcase.Elements().Any(child => child.Name.LocalName is "failure" or "error"),
        };
    }
}
