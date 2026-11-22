// FR03: Extract balance: single-caller functions added minus single-caller functions removed in one change must not exceed the limit
using Gate.Model;

namespace Gate.Rules;

internal static class FR03
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var before = SingleCaller(input.Revisions.Base);
        var after = SingleCaller(input.Revisions.Head);
        var added = after.Keys.Except(before.Keys).Select(id => after[id]).ToList();
        var removed = before.Keys.Except(after.Keys).Count();
        var limit = Settings.Limit(input.Config, "FR03");
        return added.Count - removed > limit ? [Finding(input, added, (removed, limit))] : [];
    }

    private static IReadOnlyDictionary<string, Unit> SingleCaller(Snapshot snapshot) =>
        snapshot.Units.Where(unit => !unit.Test && unit.Callers == 1).GroupBy(unit => unit.Id).ToDictionary(group => group.Key, group => group.First());

    private static Finding Finding(CheckInput input, IReadOnlyList<Unit> added, (int Removed, int Limit) measured)
    {
        var first = added[0];
        var listed = string.Join(", ", added.Select(unit => $"{unit.Id} ({unit.File}:{unit.StartLine})"));
        var message = $"FR03: the change adds {added.Count} single-caller functions and removes {measured.Removed}, balance {added.Count - measured.Removed}, limit {measured.Limit}. Added: {listed}. Inline the ones that only restate their body.";
        return new Finding("FR03", Settings.Severity(input.Config, "FR03"), new Location(first.File, first.StartLine), message)
        {
            Fingerprint = $"FR03|{input.Records.Select(change => change.Path).FirstOrDefault("")}|balance",
        };
    }
}
