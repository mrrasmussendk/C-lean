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

    /// <summary>A checkout directory that is kept between runs of the gate on one repository, so build outputs are reused.</summary>
    public static string TreeDirectory(string root, string name)
    {
        var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(root))))[..KeyLength];
        return Path.Combine(Path.GetTempPath(), "gate-trees", key, name);
    }

    private const int KeyLength = 16;

    /// <summary>Seconds on a clock that only moves forward, for measuring how long a check takes.</summary>
    public static double Seconds() => System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;

    public static string Now() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    public static string ReadInput() => Console.In.ReadToEnd();

    public static int Processors() => Environment.ProcessorCount;

    /// <summary>
    /// Whether a person is reading the output in a terminal. Pictures and colours are for them only: a pipe, a log,
    /// a terminal that asks for none (NO_COLOR, TERM=dumb) and an agent get plain lines. GATE_ART=1 or 0 decides it outright.
    /// </summary>
    public static bool Watched() =>
        Variable("GATE_ART") == "1"
        || (Variable("GATE_ART") != "0" && !Console.IsOutputRedirected && Variable("NO_COLOR").Length == 0 && Variable("TERM") != "dumb");

    public static void Print(string line) => Console.Out.WriteLine(line);

    public static void PrintError(string line) => Console.Error.WriteLine(line);
}
