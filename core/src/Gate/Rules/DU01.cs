// DU01: A change must not introduce a statement sequence whose normalized hash equals another sequence in production code
using Gate.Model;

namespace Gate.Rules;

internal static class DU01
{
    public static IReadOnlyList<Finding> Check(CheckInput input)
    {
        var revisions = input.Revisions;
        var scope = new CloneScope("DU01", Production(revisions.Base), Production(revisions.Head));
        return Clones.Findings(scope);
    }

    private static IReadOnlyList<Unit> Production(Snapshot snapshot) =>
        snapshot.Units.Where(unit => !unit.Test).ToList();
}
