namespace Gate.Effects;

/// <summary>Clock, environment and console: the remaining effect APIs the core uses.</summary>
internal static class Surroundings
{
    public static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    public static string Stamp() => DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    public static string Variable(string name) => Environment.GetEnvironmentVariable(name) ?? "";

    public static string TemporaryDirectory() =>
        Path.Combine(Path.GetTempPath(), "gate-" + Guid.NewGuid().ToString("N"));

    /// <summary>The directory the gate is installed in, and each directory above it, nearest first.</summary>
    public static IReadOnlyList<string> InstallDirectories()
    {
        var found = new List<string>();
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            found.Add(directory.FullName);
        }

        return found;
    }

    public static int Processors() => Environment.ProcessorCount;

    public static void Print(string line) => Console.Out.WriteLine(line);

    public static void PrintError(string line) => Console.Error.WriteLine(line);
}
