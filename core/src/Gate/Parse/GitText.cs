using System.Text.RegularExpressions;
using Gate.Model;

namespace Gate.Parse;

/// <summary>Output of `git log` and `git diff`, as requested by Collect/GitChange.</summary>
internal static class GitText
{
    public const string CommitMark = "\u0001";
    public const string FieldMark = "\u0002";

    private const string HunkPattern = @"^@@ -\S+ \+(?<start>\d+)(?:,(?<count>\d+))? @@";
    private const string FilePrefix = "+++ b/";

    public static IReadOnlyList<Commit> Commits(string log) =>
        log.Split(CommitMark, StringSplitOptions.RemoveEmptyEntries).Select(Commit).ToList();

    /// <summary>
    /// The lines the head has that the base has not. A file's name is read from its header only: the lines between
    /// `diff --git` and the first hunk. A changed line can look like a header (`+++ b/other`), and must not be taken for one.
    /// </summary>
    public static IReadOnlyList<LineRange> Lines(string diff) =>
        diff.Split('\n').Aggregate(new Reading("", false, []), Next).Ranges.Where(range => range.End >= range.Start).ToList();

    private sealed record Reading(string Path, bool InHeader, IReadOnlyList<LineRange> Ranges);

    private static Reading Next(Reading reading, string line) =>
        line.StartsWith(FileMark) ? reading with { Path = "", InHeader = true }
        : reading.InHeader && line.StartsWith(FilePrefix) ? reading with { Path = line[FilePrefix.Length..].TrimEnd('\t', '\r') }
        : Regex.IsMatch(line, HunkPattern) && reading.Path.Length > 0 ? reading with { InHeader = false, Ranges = [.. reading.Ranges, Range(reading.Path, line)] }
        : Regex.IsMatch(line, HunkPattern) ? reading with { InHeader = false }
        : reading;

    private const string FileMark = "diff --git ";

    private static Commit Commit(string entry)
    {
        var lines = entry.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var fields = lines[0].Split(FieldMark);
        return new Commit(fields[0], string.Join(" ", fields.Skip(1)), lines.Skip(1).ToList());
    }

    private static LineRange Range(string path, string hunk)
    {
        var match = Regex.Match(hunk, HunkPattern);
        var start = int.Parse(match.Groups["start"].Value);
        var count = match.Groups["count"].Success ? int.Parse(match.Groups["count"].Value) : 1;
        return new LineRange(path, start, start + count - 1);
    }
}
