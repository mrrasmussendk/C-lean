using System.Text.RegularExpressions;

namespace Gate.CSharp.Config;

/// <summary>Path globs: ** is any number of directories, * stays within one segment.</summary>
internal static class Globs
{
    public static Regex Compile(string glob)
    {
        var pattern = Regex.Escape(glob)
            .Replace(@"\*\*/", "(?:.*/)?")
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*");
        return new Regex($"^{pattern}$", RegexOptions.CultureInvariant);
    }
}
