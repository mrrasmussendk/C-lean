using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gate.CSharp.Config;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gate.CSharp.Analysis;

/// <summary>
/// An inline suppression as written; a missing field is the empty string. Covered is the one line it speaks for:
/// its own line when that line also holds code, else the line below.
/// </summary>
internal sealed record Suppression(
    SourceFile File,
    int Line,
    string Rule,
    string Reason,
    string Expires,
    string Approver)
{
    public int Covered { get; init; }
}

/// <summary>Finds inline suppressions in comments and applies the valid ones to findings.</summary>
internal static class Suppressions
{
    private static readonly Regex Marker = new(@"gate:allow\b(?:[ \t]+(?<rule>[A-Z]{2}\d{2})\b)?(?<rest>[^\r\n]*)");
    private static readonly Regex Reason = new("(?<![\\w-])reason=\"(?<value>[^\"]+)\"");
    private static readonly Regex Expires = new(@"(?<![\w-])expires=(?<value>\d{4}-\d{2}-\d{2})(?![\w-])");
    private static readonly Regex Approver = new(@"(?<![\w-])approver=(?<value>[^\s*]+)");

    private static readonly SyntaxKind[] CommentKinds =
    [
        SyntaxKind.SingleLineCommentTrivia, SyntaxKind.MultiLineCommentTrivia,
        SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxKind.MultiLineDocumentationCommentTrivia,
    ];

    public static IEnumerable<Suppression> In(SourceFile file) =>
        file.Tree.GetRoot()
            .DescendantTrivia()
            .Where(trivia => CommentKinds.Contains(trivia.Kind()))
            .SelectMany(trivia => Parse(file, trivia));

    public static bool IsValid(Suppression suppression, string today) =>
        RuleTable.IsKnown(suppression.Rule) && suppression.Reason != "" && suppression.Approver != ""
        && IsDate(suppression.Expires) && string.CompareOrdinal(suppression.Expires, today) >= 0;

    /// <summary>A day that exists: 2027-02-30 and 2027-13-01 have the shape of a date and are none.</summary>
    public static bool IsDate(string text) =>
        text.Length == DateLength
        && System.DateOnly.TryParseExact(
            text,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out _);

    private const int DateLength = 10;

    public static bool Covers(Suppression suppression, Finding finding) =>
        finding.Rule == suppression.Rule && finding.File == suppression.File.Path
        && finding.Line == suppression.Covered;

    public static IEnumerable<Finding> Mark(IEnumerable<Finding> findings, IReadOnlyList<Suppression> valid) =>
        findings.Select(
            finding => finding with { Suppressed = valid.Any(suppression => Covers(suppression, finding)) });

    private static IEnumerable<Suppression> Parse(SourceFile file, SyntaxTrivia trivia)
    {
        var text = trivia.ToFullString();
        var first = Lines.StartOf(trivia.GetLocation());
        return Marker.Matches(text).Select(match => new Suppression(
            file,
            first + text[..match.Index].Count(character => character == '\n'),
            match.Groups["rule"].Value,
            Field(Reason, match),
            Field(Expires, match),
            Field(Approver, match)))
            .Select(written => written with { Covered = written.Line + (HoldsCode(file, written.Line) ? 0 : 1) });
    }

    /// <summary>Whether a line holds a token of code besides its comments.</summary>
    private static bool HoldsCode(SourceFile file, int line)
    {
        var span = file.Tree.GetText().Lines[line - 1].Span;
        return file.Tree.GetRoot().DescendantTokens(span).Any(token => token.Span.OverlapsWith(span));
    }

    private static string Field(Regex field, Match marker) =>
        field.Match(marker.Groups["rest"].Value).Groups["value"].Value;
}
