using System.Text.RegularExpressions;

namespace Gate.Model;

/// <summary>Path globs: `**` crosses directories, `*` stays inside one segment.</summary>
internal static class Glob
{
    public static bool Matches(string pattern, string path) =>
        Regex.IsMatch(path, "^" + Translate(pattern) + "$");

    public static bool MatchesAny(IEnumerable<string> patterns, string path) =>
        patterns.Any(pattern => Matches(pattern, path));

    private static string Translate(string pattern) =>
        Regex.Escape(pattern)
            .Replace(@"\*\*/", "(?:.*/)?")
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*");
}
