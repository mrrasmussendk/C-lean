using System.IO;
using Gate.CSharp.Model;

namespace Gate.CSharp.Effects;

/// <summary>Writes the three interchange files. The only writer of files.</summary>
internal static class OutputWriter
{
    public static void Write(string directory, Report report)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "findings.sarif"), report.Sarif);
        File.WriteAllText(Path.Combine(directory, "graph.json"), report.Graph);
        File.WriteAllText(Path.Combine(directory, "units.json"), report.Units);
    }
}
