// TS03: DU01 applied to test code
using Gate.Model;

namespace Gate.Rules;

internal static class TS03
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var revisions = input.Revisions;
        var scope = new CloneScope("TS03", Tests(revisions.Base), Tests(revisions.Head));
        return Clones.Findings(scope);
    }

    private static IReadOnlyList<Unit> Tests(Snapshot snapshot) =>
        snapshot.Units.Where(unit => unit.Test).ToList();
}
