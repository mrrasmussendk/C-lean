using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Loading;
using Gate.CSharp.Model;

namespace Gate.CSharp.Effects;

/// <summary>Reads everything the analysis needs from the file system. The only reader of text files.</summary>
internal static class SourceReader
{
    public static Inputs Read(Options options)
    {
        var missing = new[] { options.Config, options.Architecture }.Where(path => !File.Exists(path)).ToList();
        if (missing.Count > 0 || !Directory.Exists(options.Root))
        {
            var absent = string.Join(", ", missing.DefaultIfEmpty(options.Root));
            throw new GateFailure(Failures.Line($"cannot find {absent}", "Pass existing paths to --root, --config and --architecture"));
        }
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Root));
        var tree = Files(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(Paths.IsWanted).OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new TextFile(path, File.ReadAllText(Path.Combine(root, path)))).ToList();
        return new Inputs(
            options,
            Path.GetFileName(root),
            new TextFile(options.Config, File.ReadAllText(options.Config)),
            new TextFile(options.Architecture, File.ReadAllText(options.Architecture)),
            tree);
    }

    private static IEnumerable<string> Files(string directory) =>
        Directory.EnumerateFiles(directory).Concat(
            Directory.EnumerateDirectories(directory)
                .Where(child => !Paths.SkippedDirectories.Contains(Path.GetFileName(child)))
                .SelectMany(Files));
}
