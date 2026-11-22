// HD03: A change must not modify or delete existing tests unless its change record declares it
using Gate.Model;

namespace Gate.Rules;

internal static class HD03
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var revisions = input.Revisions;
        var head = revisions.Head.Units.Where(unit => unit.Test).GroupBy(unit => unit.Id).ToDictionary(group => group.Key, group => group.First().Hash);
        var declared = input.Records.SelectMany(record => record.DeclaredTests).ToHashSet();
        return revisions.Base.Units
            .Where(unit => unit.Test && head.GetValueOrDefault(unit.Id, "") != unit.Hash)
            .Where(unit => !declared.Contains(unit.Id) && !declared.Contains(unit.File))
            .Select(unit => Finding(unit, head.ContainsKey(unit.Id) ? "modified" : "deleted"))
            .ToList();
    }

    private static Finding Finding(Unit test, string what)
    {
        var message = $"HD03: existing test {test.Id} is {what} and the change record does not declare it. Restore the test, or have the change record declare it before implementation.";
        return new Finding("HD03", Severities.Block, new Location(test.File, test.StartLine), message)
        {
            Fingerprint = $"HD03|{test.File}|{test.Id}",
        };
    }
}
