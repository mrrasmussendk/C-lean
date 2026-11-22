using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Gate.CSharp.Loading;

/// <summary>Arithmetic on root-relative paths with forward slashes.</summary>
internal static class Paths
{
    /// <summary>Directory names that are never read.</summary>
    public static readonly IReadOnlyList<string> SkippedDirectories = ["bin", ".git", "node_modules", ".vs"];

    /// <summary>The directory of a path with its trailing slash; empty for a file in the root.</summary>
    public static string Directory(string path) => path[..(path.LastIndexOf('/') + 1)];

    public static string Resolve(string directory, string relative) =>
        string.Join("/", (directory + relative.Replace('\\', '/')).Split('/').Aggregate(ImmutableList<string>.Empty, Step));

    /// <summary>The files the analysis reads: sources and project files outside obj, the restore result inside it, the glossary.</summary>
    public static bool IsWanted(string path)
    {
        if (path.Split('/').Contains("obj"))
        {
            return path.EndsWith("obj/project.assets.json");
        }
        return path.EndsWith(".cs") || path.EndsWith(".csproj") || path.EndsWith("Directory.Build.props")
            || path == Config.GlossaryReader.Path;
    }

    private static ImmutableList<string> Step(ImmutableList<string> walked, string segment) => segment switch
    {
        "." or "" => walked,
        ".." when walked.Count > 0 => walked.RemoveAt(walked.Count - 1),
        _ => walked.Add(segment),
    };
}
