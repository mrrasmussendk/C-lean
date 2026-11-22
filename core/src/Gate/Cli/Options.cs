namespace Gate.Cli;

/// <summary>Reads named options out of the command line.</summary>
internal static class Options
{
    public static string Value(IReadOnlyList<string> arguments, string name, string fallback)
    {
        var index = arguments.ToList().IndexOf(name);
        return index >= 0 && index + 1 < arguments.Count ? arguments[index + 1] : fallback;
    }

    public static bool Has(IReadOnlyList<string> arguments, string name) => arguments.Contains(name);

    public static string Root(IReadOnlyList<string> arguments) => Value(arguments, "--root", ".");

    /// <summary>The words after the command that are neither an option nor an option's value.</summary>
    public static IReadOnlyList<string> Words(IReadOnlyList<string> arguments) =>
        arguments.Where((word, index) => !word.StartsWith("--") && (index == 0 || !arguments[index - 1].StartsWith("--") || Switches.Contains(arguments[index - 1]))).ToList();

    private static readonly IReadOnlyList<string> Switches = ["--offline"];
}
