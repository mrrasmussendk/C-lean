using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gate.CSharp.Loading;

/// <summary>
/// The conditional-compilation symbols of a project, as an SDK Debug build defines them: DEBUG and TRACE, the
/// symbols of the target framework (NET, NET10_0, NET5_0_OR_GREATER up to its own version) and DefineConstants.
/// Without them the code under `#if NET8_0_OR_GREATER` would not be read at all.
/// </summary>
internal static class Preprocessor
{
    /// <summary>The first version named net&lt;major&gt;.&lt;minor&gt;; the OR_GREATER symbols start here.</summary>
    private const int FirstVersion = 5;

    private static readonly Regex Framework = new(@"^net(?<major>\d{1,3})\.(?<minor>\d{1,3})");
    private static readonly Regex Symbol = new(@"^[\p{L}_][\p{L}\p{Nd}_]*$");

    public static readonly IReadOnlyList<string> Build = ["DEBUG", "TRACE"];

    /// <summary>`framework` is the value of TargetFramework, or of TargetFrameworks, whose first entry
    /// counts.</summary>
    public static IReadOnlyList<string> Symbols(string framework, string constants) =>
        [.. Build, .. OfFramework(Framework.Match(framework.Trim())), .. Defined(constants)];

    private static IEnumerable<string> OfFramework(Match framework)
    {
        if (!framework.Success)
        {
            return [];
        }
        var major = int.Parse(framework.Groups["major"].Value, CultureInfo.InvariantCulture);
        var later = Enumerable.Range(FirstVersion, System.Math.Max(0, major - FirstVersion + 1))
            .Select(version => $"NET{version}_0_OR_GREATER");
        return new[] { "NET", $"NET{major}_{framework.Groups["minor"].Value}" }.Concat(later);
    }

    /// <summary>The entries of DefineConstants that are symbols; `$(DefineConstants)` and the like are not.</summary>
    private static IEnumerable<string> Defined(string constants) =>
        constants.Split(';', ',').Select(entry => entry.Trim()).Where(entry => Symbol.IsMatch(entry));
}
