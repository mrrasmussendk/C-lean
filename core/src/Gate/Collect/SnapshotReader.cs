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

    /// <summary>
    /// Fails the gate unless every language wrote its three files, each a JSON object with its list in it.
    /// A file that is missing or has another shape would otherwise read as no findings, and a failure would pass.
    /// </summary>
    public static void Require(string directory, IReadOnlyList<string> languages)
    {
        var wrong = from language in languages
                    from file in Required
                    let path = Path.Combine(directory, language, file.Name)
                    where !Documents.Read(path, text => InterchangeText.Holds(text, file.List))
                    select $"{path}: the file is missing or is not a JSON object with `{file.List}`";
        var problems = wrong.ToList();
        if (problems.Count > 0)
        {
            throw new GateFailure(string.Join("\n      ", problems) + ". The language package writes it; run `c-lean check` without --offline, or `c-lean doctor` to see what is installed.");
        }
    }

    private static readonly IReadOnlyList<(string Name, string List)> Required = [("findings.sarif", "runs"), ("graph.json", "nodes"), ("units.json", "units")];

    private static IReadOnlyList<T> Each<T>(string directory, IReadOnlyList<string> languages, string file, Func<string, IReadOnlyList<T>> parse) =>
        languages.SelectMany(language => Documents.Read(Path.Combine(directory, language, file), parse)).ToList();
}
