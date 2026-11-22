using Gate.Contracts;
using Gate.Effects;

namespace Gate.Cli;

/// <summary>The command table of the spec's CLI section.</summary>
internal static class Commands
{
    private const string Usage = "usage: gate init | check [--stage rules|delta|tests|trace|judge] [--base <ref>] [--offline] | change new <id> | change approve <id> | tests prune | judge mint | conflict \"<text>\" | explain <rule ID> | doctor";

    public static int Run(IReadOnlyList<string> arguments)
    {
        var words = Options.Words(arguments);
        var command = string.Join(' ', words.Take(Depth(words)));
        var rest = words.Skip(Depth(words)).ToList();
        return command switch
        {
            "init" => InitCommand.Run(arguments),
            "check" => CheckCommand.Run(arguments),
            "change new" => ChangeCommands.New(arguments, rest),
            "change approve" => ChangeCommands.Approve(arguments, rest),
            "tests prune" => PruneCommand.Run(arguments),
            "judge mint" => MintCommand.Run(arguments),
            "conflict" => ChangeCommands.Conflict(arguments, rest),
            "explain" => ExplainCommand.Run(rest),
            "doctor" => DoctorCommand.Run(arguments),
            _ => UnknownCommand(),
        };
    }

    private static int Depth(IReadOnlyList<string> words) =>
        words.Count > 0 && TwoWord.Contains(words[0]) ? TwoWords : Math.Min(words.Count, 1);

    private static int UnknownCommand()
    {
        Surroundings.PrintError(Usage);
        return C6Hardening.GateFailed;
    }

    private const int TwoWords = 2;
    private static readonly IReadOnlyList<string> TwoWord = ["change", "tests", "judge"];
}
