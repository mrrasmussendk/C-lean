using Gate.Contracts;
using Gate.Effects;

namespace Gate.Cli;

/// <summary>The command table of the spec's CLI section.</summary>
internal static class Commands
{
    public static int Run(IReadOnlyList<string> arguments)
    {
        var words = Options.Words(arguments);
        var command = Asked(arguments).Length > 0 ? Asked(arguments) : string.Join(' ', words.Take(Depth(words)));
        var rest = words.Skip(Depth(words)).ToList();
        return command switch
        {
            "init" => InitCommand.Run(arguments),
            "check" => CheckCommand.Run(arguments),
            "change new" => ChangeCommands.New(arguments, rest),
            "change approve" => ChangeCommands.Approve(arguments, rest),
            "tests prune" => PruneCommand.Run(arguments),
            "tests nightly" => PruneCommand.Nightly(arguments),
            "judge mint" => MintCommand.Run(arguments),
            "conflict" => ChangeCommands.Conflict(arguments, rest),
            "explain" => ExplainCommand.Run(rest),
            "doctor" => DoctorCommand.Run(arguments),
            "hook" => HookCommand.Run(),
            "stats" => StatsCommand.Run(arguments),
            "instructions" => InitCommand.Instruct(arguments),
            "" or "help" => Guide.Help(arguments),
            "version" => Guide.Version(),
            _ => UnknownCommand(command),
        };
    }

    private static int Depth(IReadOnlyList<string> words) =>
        words.Count > 0 && TwoWord.Contains(words[0]) ? TwoWords : Math.Min(words.Count, 1);

    private static int UnknownCommand(string command)
    {
        Surroundings.PrintError($"gate: '{command}' is not a command. Run `gate help` for the commands and the next step in this repository.");
        return C6Hardening.GateFailed;
    }

    /// <summary>`--help`, `-h` and `--version` anywhere on the line, read as the commands they mean; else empty.</summary>
    private static string Asked(IReadOnlyList<string> arguments) =>
        arguments.Contains("--help") || arguments.Contains("-h") ? "help" : arguments.Contains("--version") ? "version" : "";

    private const int TwoWords = 2;
    private static readonly IReadOnlyList<string> TwoWord = ["change", "tests", "judge"];
}
