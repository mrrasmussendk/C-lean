using Gate.Model;

namespace Gate.Rules;

internal sealed record CloneScope(string Rule, IReadOnlyList<Unit> Base, IReadOnlyList<Unit> Head);

internal sealed record Occurrence(Unit Unit, Sequence Sequence);

/// <summary>Clone detection on statement-sequence hashes, shared by DU01 and TS03.</summary>
internal static class Clones
{
    public static IReadOnlyList<Finding> Findings(CloneScope scope)
    {
        var known = scope.Base.SelectMany(unit => unit.Sequences.Select(sequence => Key(unit, sequence))).ToHashSet();
        return scope.Head
            .SelectMany(unit => unit.Sequences.Select(sequence => new Occurrence(unit, sequence)))
            .GroupBy(occurrence => Language(occurrence.Unit) + occurrence.Sequence.Hash)
            .SelectMany(group => Pairs(group.ToList()))
            .Select(pair => Finding(scope.Rule, pair, known))
            .DistinctBy(finding => finding.Fingerprint)
            .ToList();
    }

    private static IEnumerable<(Occurrence Here, Occurrence Other)> Pairs(IReadOnlyList<Occurrence> group) =>
        from here in @group
        let other = @group.FirstOrDefault(candidate => !Overlaps(here, candidate))
        where other is not null
        select (here, other);

    private static bool Overlaps(Occurrence left, Occurrence right) =>
        left.Unit.Id == right.Unit.Id
        && left.Sequence.StartLine <= right.Sequence.EndLine
        && right.Sequence.StartLine <= left.Sequence.EndLine;

    private static Finding Finding(string rule, (Occurrence Here, Occurrence Other) pair, IReadOnlySet<string> known)
    {
        var introduced = !known.Contains(Key(pair.Here.Unit, pair.Here.Sequence));
        var where = new Location(pair.Here.Unit.File, pair.Here.Sequence.StartLine);
        var other = $"{pair.Other.Unit.File}:{pair.Other.Sequence.StartLine}";
        var message = $"{rule}: statements at line {where.Line} repeat the sequence at {other}. Call the existing code, or extract the shared sequence into one function used by both places.";
        return new Finding(rule, introduced ? Severities.Block : Severities.Report, where, message)
        {
            Fingerprint = $"{rule}|{pair.Here.Unit.Id}|{pair.Other.Unit.Id}",
        };
    }

    private static string Key(Unit unit, Sequence sequence) => $"{unit.Id}|{sequence.Hash}";

    private static string Language(Unit unit) => unit.Id.Split(':')[0];
}
