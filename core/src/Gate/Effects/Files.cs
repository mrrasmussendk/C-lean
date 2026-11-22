namespace Gate.Effects;

/// <summary>The reader and writer: the only code in the core that touches the file system.</summary>
internal static class Files
{
    /// <summary>The file's text, or empty when there is no such file.</summary>
    public static string Read(string path) =>
        File.Exists(path) ? File.ReadAllText(path) : "";

    public static bool Exists(string path) => File.Exists(path);

    public static string Absolute(string path) => Path.GetFullPath(path);

    public static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        File.WriteAllText(path, text);
    }

    /// <summary>Root-relative paths, forward slashes, of every file under the root, without tool and build directories.</summary>
    public static IReadOnlyList<string> Tree(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !Skipped.Any(skipped => $"/{path}".Contains($"/{skipped}/")))
            .Order()
            .ToList();

    public static void RemoveDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private static readonly IReadOnlyList<string> Skipped = [".git", ".gate", "node_modules", "bin", "obj", "dist", "__pycache__"];
}
