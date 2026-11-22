using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Reads what the native packages wrote for one revision and merges the languages.</summary>
internal static class SnapshotReader
{
    public static Snapshot Read(string directory, IReadOnlyList<string> languages)
    {
        var graphs = languages.Select(language => InterchangeText.Graph(Text(directory, language, "graph.json"))).ToList();
        var graph = new Graph(graphs.SelectMany(part => part.Nodes).ToList(), graphs.SelectMany(part => part.Edges).ToList());
        var reports = languages.Select(language => Text(directory, language, "mutation.json")).ToList();
        return new Snapshot(Each(directory, languages, "findings.sarif", InterchangeText.Findings), graph, Each(directory, languages, "units.json", InterchangeText.Units))
        {
            HasMutationReport = reports.Any(report => report.Length > 0),
            Mutants = reports.SelectMany(MutationText.Mutants).ToList(),
            Tests = Each(directory, languages, "tests.xml", InterchangeText.Tests),
            Spans = Each(directory, languages, "traces.json", InterchangeText.Spans),
        };
    }

    private static IReadOnlyList<T> Each<T>(string directory, IReadOnlyList<string> languages, string file, Func<string, IReadOnlyList<T>> parse) =>
        languages.SelectMany(language => parse(Text(directory, language, file))).ToList();

    private static string Text(string directory, string language, string file) =>
        Files.Read(Path.Combine(directory, language, file));
}
