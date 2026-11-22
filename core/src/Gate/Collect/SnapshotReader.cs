using Gate.Effects;
using Gate.Model;
using Gate.Parse;

namespace Gate.Collect;

/// <summary>Reads what the native packages wrote for one revision and merges the languages.</summary>
internal static class SnapshotReader
{
    public static Snapshot Read(string directory, IReadOnlyList<string> languages)
    {
        var graphs = Each(directory, languages, "graph.json", text => new[] { InterchangeText.Graph(text) });
        var graph = new Graph(graphs.SelectMany(part => part.Nodes).ToList(), graphs.SelectMany(part => part.Edges).ToList());
        var reports = languages.Select(language => Files.Read(Path.Combine(directory, language, "mutation.json"))).ToList();
        return new Snapshot(Each(directory, languages, "findings.sarif", InterchangeText.Findings), graph, Each(directory, languages, "units.json", InterchangeText.Units))
        {
            HasMutationReport = reports.Any(report => report.Length > 0),
            Mutants = Each(directory, languages, "mutation.json", MutationText.Mutants),
            Tests = Each(directory, languages, "tests.xml", InterchangeText.Tests),
            Spans = Each(directory, languages, "traces.json", InterchangeText.Spans),
        };
    }

    private static IReadOnlyList<T> Each<T>(string directory, IReadOnlyList<string> languages, string file, Func<string, IReadOnlyList<T>> parse) =>
        languages.SelectMany(language => Documents.Read(Path.Combine(directory, language, file), parse)).ToList();
}
