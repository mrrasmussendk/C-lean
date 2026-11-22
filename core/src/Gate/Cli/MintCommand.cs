using Gate.Collect;
using Gate.Contracts;
using Gate.Effects;
using Gate.Model;

namespace Gate.Cli;

internal sealed record Minting(string Root, JudgeSettings Judge, string History);

/// <summary>`gate judge mint`: votes on every unjudged function and question. CI only.</summary>
internal static class MintCommand
{
    private const string RationalePrompt = "The function below failed this check. Explain why in two sentences.";

    public static int Run(IReadOnlyList<string> arguments)
    {
        var input = CheckCommand.Collect(arguments);
        var judge = input.Config.Judge;
        var missing = Missing(judge);
        if (missing.Length > 0)
        {
            throw new GateFailure($"judge mint refused: {missing}");
        }

        var minting = new Minting(Options.Value(arguments, Options.RootOption, Options.Here), judge, Files.Read(judge.FailHistory));
        var ballots = C5VerdictLockfile.Unjudged(input).Where(ballot => !minting.History.Contains(HistoryKey(ballot))).ToList();
        foreach (var ballot in ballots)
        {
            Mint(minting, ballot);
        }

        Surroundings.Print($"gate: minted {ballots.Count} verdicts");
        return C6Hardening.Passed;
    }

    private static string Missing(JudgeSettings judge) =>
        Surroundings.Variable("CI") != "true" ? "the environment variable CI is not `true`. Verdicts are minted in CI only, so a local run cannot re-roll one."
        : judge.Command.Count == 0 ? "gate.yaml has no `judge.command`. Set it to the command that reads a prompt on standard input and answers pass or fail."
        : judge.FailHistory.Length == 0 ? "gate.yaml has no `judge.fail_history`. Set it to a file outside the repository; fail verdicts are kept there."
        : "";

    private static void Mint(Minting minting, Ballot ballot)
    {
        var source = Source(minting.Root, ballot.Unit);
        var votes = Enumerable.Range(0, minting.Judge.Votes).Select(_ => Vote(minting.Judge, ballot, source)).ToList();
        var split = C5VerdictLockfile.Split(votes);
        if (C5VerdictLockfile.Outcome(votes) == C5VerdictLockfile.Pass)
        {
            var path = Path.Combine(minting.Root, Layout.Lock);
            var fingerprint = C5VerdictLockfile.Fingerprint(minting.Judge, ballot.Question);
            Files.Write(path, $"{Existing(path)}  - {{hash: {ballot.Unit.Hash}, question: {ballot.Question.Id}, fingerprint: \"{fingerprint}\", split: {split}}}\n");
            return;
        }

        var rationale = Ask(minting.Judge, $"{RationalePrompt}\n{ballot.Question.Prompt}\n\n{source}").ReplaceLineEndings(" ");
        Files.Write(minting.Judge.FailHistory, $"{Files.Read(minting.Judge.FailHistory)}- {{key: \"{HistoryKey(ballot)}\", unit: \"{ballot.Unit.Id}\", split: {split}, rationale: \"{rationale.Replace("\"", "'")}\"}}\n");
        Surroundings.Print($"advisory {ballot.Question.Id} fail {ballot.Unit.File}:{ballot.Unit.StartLine} {rationale}");
    }

    /// <summary>Zero-shot. J01 is asked as suggest-then-compare: the judge names the body before it sees the real name.</summary>
    private static string Vote(JudgeSettings judge, Ballot ballot, string source)
    {
        var subject = ballot.Question.Id == "J01"
            ? $"{Ask(judge, $"{C5VerdictLockfile.SuggestPrompt}\n\n{Body(source)}").Trim()}\n{ballot.Unit.Id}"
            : source;
        var answer = Ask(judge, $"{ballot.Question.Prompt}\n\n{subject}").Trim().ToLowerInvariant();
        return answer.StartsWith(C5VerdictLockfile.Fail) ? C5VerdictLockfile.Fail : C5VerdictLockfile.Pass;
    }

    private static string Ask(JudgeSettings judge, string prompt)
    {
        var call = new ProcessCall(judge.Command[0], judge.Command.Skip(1).ToList(), ".") { Input = prompt };
        var result = Processes.Run(call);
        return result.ExitCode == 0 ? result.Output : throw new GateFailure($"judge command `{string.Join(' ', judge.Command)}` failed with exit code {result.ExitCode}: {result.Error.Trim()}");
    }

    private static string Source(string root, Unit unit) =>
        string.Join('\n', Files.Read(Path.Combine(root, unit.File)).Split('\n').Skip(unit.StartLine - 1).Take(unit.EndLine - unit.StartLine + 1));

    private static string Body(string source) => string.Join('\n', source.Split('\n').Skip(1));

    private static string Existing(string path) =>
        Files.Exists(path) ? Files.Read(path) : "verdicts:\n";

    private static string HistoryKey(Ballot ballot) => $"{ballot.Unit.Hash}|{ballot.Question.Id}";
}
