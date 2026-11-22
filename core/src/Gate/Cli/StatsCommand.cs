using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Parse;

namespace Gate.Cli;

/// <summary>`gate stats`: what the gate has cost in this working copy, and which rules cost the most iterations.</summary>
internal static class StatsCommand
{
    public static int Run(IReadOnlyList<string> arguments)
    {
        var root = Options.Value(arguments, Options.RootOption, Options.Here);
        var path = Path.Combine(root, Layout.Log);
        if (!Files.Exists(path))
        {
            throw new GateFailure($"{path}: there is no log yet. `gate check` and the agent hook write one line per check.");
        }

        foreach (var line in Stats.Summary(LogText.Runs(Files.Read(path)), Files.Count(Path.Combine(root, Layout.Conflicts))))
        {
            Surroundings.Print(line);
        }

        return C6Hardening.Passed;
    }
}
