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

    public static IReadOnlyList<LineRange> Lines(string diff)
    {
        var lines = diff.Split('\n');
        var files = lines.Select((line, index) => (line, index)).Where(pair => pair.line.StartsWith(FilePrefix)).ToList();
        return lines.Select((line, index) => (line, index))
            .Where(pair => Regex.IsMatch(pair.line, HunkPattern))
            .Select(pair => Range(files.Last(file => file.index < pair.index).line[FilePrefix.Length..], pair.line))
            .Where(range => range.End >= range.Start)
            .ToList();
    }

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
