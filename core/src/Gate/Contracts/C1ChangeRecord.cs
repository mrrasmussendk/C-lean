// C1 Change record: every change carries one approved change.yaml with exactly one reason.
using System.Security.Cryptography;
using System.Text;
using Gate.Model;

namespace Gate.Contracts;

internal static class C1ChangeRecord
{
    public const string Directory = "changes/";

    private static readonly IReadOnlyList<string> Taxonomy = ["feature", "fix", "refactor", "dependency", "performance", "compliance"];

    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var records = input.Records;
        if (records.Count != 1)
        {
            return [Blocking.At("C1.record", new Location(Directory, 0), $"the change touches {records.Count} change records under {Directory} ({string.Join(", ", records.Select(record => record.Path).DefaultIfEmpty("none"))}), required exactly 1. A change that has none needs `gate change new <id>` and an approval before implementation; a change that needs two is two changes.")];
        }

        var problems = Problems(input.Config, records[0]).Select(problem => Blocking.At(problem.Rule, new Location(records[0].Path, 0), problem.Message));
        return [.. problems, .. records[0].Acceptance.Select(criterion => Criterion(records[0], criterion))];
    }

    /// <summary>An acceptance criterion is shown on every check, for the reviewer. Whether it is met is a human's call.</summary>
    private static Finding Criterion(ChangeRecord record, string criterion) =>
        new("C1.acceptance", Severities.Report, new Location(record.Path, 0), $"C1.acceptance: the change must satisfy: {criterion}. A reviewer confirms this before merge; the gate does not decide it.")
        {
            Fingerprint = $"C1.acceptance|{record.Path}|{criterion}",
        };

    /// <summary>The digest an approval signs: every field of the record except the approval itself.</summary>
    public static string Digest(ChangeRecord record)
    {
        var delta = record.Delta;
        var fields = new[] { record.Id, string.Join(",", record.Reasons), record.Actor, Edges(delta.Add), Edges(delta.Remove), string.Join(",", record.DeclaredTests) };
        var parts = record.Acceptance.Count == 0 ? fields : [.. fields, string.Join("\n", record.Acceptance)];
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", parts)))).ToLowerInvariant();
    }

    private static IEnumerable<(string Rule, string Message)> Problems(Config config, ChangeRecord record)
    {
        if (record.Reasons.Count != 1 || !Taxonomy.Contains(record.Reasons[0]))
        {
            yield return ("C1.reason", $"`reason` holds {record.Reasons.Count} values ({string.Join(", ", record.Reasons.DefaultIfEmpty("none"))}), required exactly 1 of {string.Join(", ", Taxonomy)}. A change with two reasons is two changes.");
        }

        if (record.Actor.Length == 0)
        {
            yield return ("C1.actor", "`actor` is empty. Name who asked for the change.");
        }

        foreach (var problem in ApprovalProblems(config, record))
        {
            yield return problem;
        }
    }

    private static IEnumerable<(string Rule, string Message)> ApprovalProblems(Config config, ChangeRecord record)
    {
        if (record.Approval is not { } approval)
        {
            yield return ("C1.approval", $"there is no `approval` block. Someone other than the implementer runs `gate change approve {record.Id}` before implementation starts.");
            yield break;
        }

        if (config.Implementers.Contains(approval.Approver))
        {
            yield return ("C1.approval", $"`approval.approver` is '{approval.Approver}', which gate.yaml lists under `implementers`. Someone else must approve.");
        }

        if (approval.Digest != Digest(record))
        {
            yield return ("C1.edited", $"`approval.digest` is {approval.Digest} and the record's fields now hash to {Digest(record)}: the record was edited after {approval.Approver} approved it on {approval.At}. Have it approved again.");
        }
    }

    private static string Edges(IReadOnlyList<ComponentEdge> edges) =>
        string.Join(",", edges.Select(edge => $"{edge.From}>{edge.To}").Order());
}
