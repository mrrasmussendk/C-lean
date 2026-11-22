namespace Gate.Effects;

/// <summary>Clock, environment and console: the remaining effect APIs the core uses.</summary>
internal static class Surroundings
{
    public static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    public static string Stamp() => DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    public static string Variable(string name) => Environment.GetEnvironmentVariable(name) ?? "";

    public static string TemporaryDirectory() =>
        Path.Combine(Path.GetTempPath(), "gate-" + Guid.NewGuid().ToString("N"));

    public static void Print(string line) => Console.Out.WriteLine(line);

    public static void PrintError(string line) => Console.Error.WriteLine(line);
}
