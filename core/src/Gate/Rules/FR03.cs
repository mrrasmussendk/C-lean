// FR03: Extract balance: single-caller functions added minus single-caller functions removed in one change must not exceed the limit
using Gate.Model;

namespace Gate.Rules;

internal static class FR03
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var before = SingleCaller(input.Revisions.Base);
        var after = SingleCaller(input.Revisions.Head);
        var added = after.Except(before).Count();
        var removed = before.Except(after).Count();
        var limit = Settings.Limit(input.Config, "FR03");
        return added - removed > limit ? [Finding(input, (added, removed, limit))] : [];
    }

    private static IReadOnlySet<string> SingleCaller(Snapshot snapshot) =>
        snapshot.Units.Where(unit => !unit.Test && unit.Callers == 1).Select(unit => unit.Id).ToHashSet();

    private static Finding Finding(CheckInput input, (int Added, int Removed, int Limit) measured)
    {
        var record = input.Records.Select(change => change.Path).FirstOrDefault("");
        var message = $"FR03: the change adds {measured.Added} single-caller functions and removes {measured.Removed}, balance {measured.Added - measured.Removed}, limit {measured.Limit}. Inline the extractions that only restate their body.";
        return new Finding("FR03", Settings.Severity(input.Config, "FR03"), new Location(record, 0), message)
        {
            Fingerprint = $"FR03|{record}|balance",
        };
    }
}
