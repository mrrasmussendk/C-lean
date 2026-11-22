using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gate.CSharp.Analysis;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;

namespace Gate.CSharp.Effects;

/// <summary>Reads everything a command needs from the file system. The only reader of text files.</summary>
internal static class SourceReader
{
    public static Inputs Read(Options options)
    {
        var named = new[] { options.Config, options.Architecture, options.Lines }.Where(path => path != "");
        var missing = named.Where(path => !File.Exists(path)).ToList();
        if (missing.Count > 0 || !Directory.Exists(options.Root))
        {
            var absent = string.Join(", ", missing.DefaultIfEmpty(options.Root));
            throw new GateFailure(Failures.Line($"cannot find {absent}", "Pass existing paths to --root, --config, --architecture and --lines"));
        }
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Root));
        var tree = Files(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(Paths.IsWanted).OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => TextOf(Path.Combine(root, path), path)).ToList();
        return new Inputs(
            options,
            root,
            Path.GetFileName(root),
            TextOf(options.Config, options.Config),
            TextOf(options.Architecture, options.Architecture),
            options.Lines == "" ? new TextFile("", "") : TextOf(options.Lines, options.Lines),
            tree);
    }

    /// <summary>The file as UTF-8 text, with the length of its byte order mark kept apart.</summary>
    private static TextFile TextOf(string location, string name)
    {
        var bytes = File.ReadAllBytes(location);
        var mark = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble) ? Encoding.UTF8.Preamble.Length : 0;
        return new TextFile(name, Encoding.UTF8.GetString(bytes, mark, bytes.Length - mark), mark);
    }

    private static IEnumerable<string> Files(string directory) =>
        Directory.EnumerateFiles(directory).Concat(
            Directory.EnumerateDirectories(directory)
                .Where(child => !Paths.SkippedDirectories.Contains(Path.GetFileName(child)))
                .SelectMany(Files));
}
