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
            return [Blocking.At("C1.record", new Location(Directory, 0), $"the change touches {records.Count} change records under {Directory} ({string.Join(", ", records.Select(record => record.Path).DefaultIfEmpty("none"))}), required exactly 1. A change that has none needs `c-lean change new <id>` and an approval before implementation; a change that needs two is two changes.")];
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

    /// <summary>
    /// The digest an approval signs: every field of the record except the approval itself. Each value is written
    /// with its length in front, so no value can be shaped to read as two, and the tests to modify and the tests
    /// to delete are separate fields, so moving a test from one list to the other changes the digest.
    /// </summary>
    public static string Digest(ChangeRecord record)
    {
        var delta = record.Delta;
        var modified = record.DeclaredTests.Except(record.DeletedTests).ToList();
        var fields = new[] { [record.Id], record.Reasons, [record.Actor], Edges(delta.Add), Edges(delta.Remove), modified, record.DeletedTests, record.Acceptance };
        var text = string.Concat(fields.Select(field => $"[{field.Count}]" + string.Concat(field.Select(value => $"{value.Length}:{value}"))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
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
            yield return ("C1.approval", $"there is no `approval` block. Someone other than the implementer runs `c-lean change approve {record.Id}` before implementation starts.");
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

    private static IReadOnlyList<string> Edges(IReadOnlyList<ComponentEdge> edges) =>
        edges.Select(edge => $"{edge.From}>{edge.To}").Order().ToList();
}
