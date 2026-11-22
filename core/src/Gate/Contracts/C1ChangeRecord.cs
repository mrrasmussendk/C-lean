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
            return [Blocking.At("C1.record", new Location(Directory, 0), $"the change carries {records.Count} change records, required 1. A change that needs two reasons is two changes; run `gate change new` for a change that has none.")];
        }

        return Problems(input.Config, records[0]).Select(problem => Blocking.At(problem.Rule, new Location(records[0].Path, 0), problem.Message)).ToList();
    }

    /// <summary>The digest an approval signs: every field of the record except the approval itself.</summary>
    public static string Digest(ChangeRecord record)
    {
        var delta = record.Delta;
        var parts = new[] { record.Id, string.Join(",", record.Reasons), record.Actor, Edges(delta.Add), Edges(delta.Remove), string.Join(",", record.DeclaredTests) };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", parts)))).ToLowerInvariant();
    }

    private static IEnumerable<(string Rule, string Message)> Problems(Config config, ChangeRecord record)
    {
        if (record.Reasons.Count != 1 || !Taxonomy.Contains(record.Reasons[0]))
        {
            yield return ("C1.reason", $"the record names {record.Reasons.Count} reasons ({string.Join(", ", record.Reasons)}), required exactly 1 of {string.Join(", ", Taxonomy)}. Split the change.");
        }

        if (record.Actor.Length == 0)
        {
            yield return ("C1.actor", "the record names no actor. Name who asked for the change.");
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
            yield return ("C1.approval", "the record is not approved. Someone other than the implementer runs `gate change approve`.");
            yield break;
        }

        if (config.Implementers.Contains(approval.Approver))
        {
            yield return ("C1.approval", $"the record is approved by implementer '{approval.Approver}'. Someone else must approve.");
        }

        if (approval.Digest != Digest(record))
        {
            yield return ("C1.edited", "the record was edited after approval. Have it approved again.");
        }
    }

    private static string Edges(IReadOnlyList<ComponentEdge> edges) =>
        string.Join(",", edges.Select(edge => $"{edge.From}>{edge.To}").Order());
}
