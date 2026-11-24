using Gate.Contracts;
using Gate.Model;
using Gate.Rules;

namespace Gate.Cli;

internal sealed record Stage(string Name, Func<CheckInput, IReadOnlyList<Finding>> Check);

/// <summary>The stages of `c-lean check`, in the order they run. Each is a function of the input.</summary>
internal static class Stages
{
    public const string All = "all";

    public static readonly IReadOnlyList<Stage> Named =
    [
        new("rules", Rules),
        new("delta", C2ArchitectureDelta.Check),
        new("tests", C4TestAdmission.Check),
        new("trace", C3TraceCheck.Check),
        new("judge", C5VerdictLockfile.Check),
    ];

    public static IReadOnlyList<Finding> Run(CheckInput input, string stage) =>
        Named.Where(named => stage == All || named.Name == stage)
            .SelectMany(named => named.Check(input))
            .ToList();

    private static IReadOnlyList<Finding> Rules(CheckInput input) =>
    [
        .. input.Revisions.Head.Findings,
        .. C1ChangeRecord.Check(input),
        .. HD02.Check(input),
        .. HD03.Check(input),
        .. DU01.Check(input),
        .. TS03.Check(input),
        .. FR01.Check(input),
        .. FR03.Check(input),
        .. FR04.Check(input),
        .. Responsibility.Check(input),
        .. Custom.Check(input),
    ];
}
