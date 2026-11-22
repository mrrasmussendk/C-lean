// C5 Verdict lockfile: judge verdicts are keyed, minted by vote and reproducible. Advisory in this version.
using System.Security.Cryptography;
using System.Text;
using Gate.Model;

namespace Gate.Contracts;

internal sealed record Question(string Id, string Prompt);

internal sealed record Ballot(Unit Unit, Question Question);

internal static class C5VerdictLockfile
{
    public const string RubricVersion = "1";
    public const string Pass = "pass";
    public const string Fail = "fail";

    private const string Answer = " Answer with the single word pass or fail.";

    public static readonly IReadOnlyList<Question> Questions =
    [
        new("J01", "Two names for one function follow, the first suggested from its body alone, the second its actual name. Pass if they mean the same thing." + Answer),
        new("J02", "Pass if all statements in the function are at one level of abstraction." + Answer),
        new("J03", "Fail if a meaningfully named function can be extracted from this one." + Answer),
        new("J04", "Fail if any comment in the function restates the code or contradicts it." + Answer),
        new("J05", "Fail if a parameter or field uses a primitive where a domain type exists or is warranted." + Answer),
        new("J06", "Pass if this function adds behaviour beyond forwarding." + Answer),
    ];

    public const string SuggestPrompt = "Suggest a name for the function whose body follows. Answer with the name only.";

    /// <summary>New or changed production functions that have no verdict in the lockfile for a question.</summary>
    public static IReadOnlyList<Ballot> Unjudged(CheckInput input)
    {
        var known = input.Revisions.Base.Units.Select(unit => unit.Hash).ToHashSet();
        var judged = input.Protected.Lock.Select(verdict => Key(verdict.Hash, verdict.Question, verdict.Fingerprint)).ToHashSet();
        return input.Revisions.Head.Units
            .Where(unit => !unit.Test && !known.Contains(unit.Hash))
            .SelectMany(unit => Questions.Select(question => new Ballot(unit, question)))
            .Where(ballot => !judged.Contains(Key(ballot.Unit.Hash, ballot.Question.Id, Fingerprint(input.Config.Judge, ballot.Question))))
            .ToList();
    }

    public static IReadOnlyList<Finding> Check(CheckInput input) =>
        Unjudged(input)
            .GroupBy(ballot => ballot.Unit.Id)
            .Select(group => Advisory(group.First().Unit, group.Count()))
            .ToList();

    /// <summary>Model ID, prompt hash and vote count: what makes a verdict reproducible.</summary>
    public static string Fingerprint(JudgeSettings judge, Question question)
    {
        var prompt = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(question.Prompt))).ToLowerInvariant();
        return $"{judge.Model}/{prompt}/{judge.Votes}/rubric-{RubricVersion}";
    }

    /// <summary>A vote that is not unanimous is a pass; only a unanimous fail is a fail.</summary>
    public static string Outcome(IReadOnlyList<string> votes) =>
        votes.Count > 0 && votes.All(vote => vote == Fail) ? Fail : Pass;

    public static string Split(IReadOnlyList<string> votes) =>
        $"{votes.Count(vote => vote != Fail)}-{votes.Count(vote => vote == Fail)}";

    private static string Key(string hash, string question, string fingerprint) => $"{hash}|{question}|{fingerprint}";

    private static Finding Advisory(Unit unit, int questions) =>
        new("C5.unjudged", Severities.Report, new Location(unit.File, unit.StartLine), $"C5.unjudged: {unit.Id} has no verdict for {questions} judge questions. CI mints them with `gate judge mint`.")
        {
            Fingerprint = $"C5.unjudged|{unit.Id}",
        };
}
