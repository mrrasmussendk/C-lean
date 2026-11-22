using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gate.CSharp.Config;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gate.CSharp.Analysis;

/// <summary>An inline suppression as written; a missing field is the empty string.</summary>
internal sealed record Suppression(
    SourceFile File,
    int Line,
    string Rule,
    string Reason,
    string Expires,
    string Approver);

/// <summary>Finds inline suppressions in comments and applies the valid ones to findings.</summary>
internal static class Suppressions
{
    private static readonly Regex Marker = new(@"gate:allow\b(?:[ \t]+(?<rule>[A-Z]{2}\d{2})\b)?(?<rest>[^\r\n]*)");
    private static readonly Regex Reason = new("reason=\"(?<value>[^\"]+)\"");
    private static readonly Regex Expires = new(@"expires=(?<value>\d{4}-\d{2}-\d{2})");
    private static readonly Regex Approver = new(@"approver=(?<value>[^\s*]+)");

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
        && suppression.Expires != "" && string.CompareOrdinal(suppression.Expires, today) >= 0;

    public static bool Covers(Suppression suppression, Finding finding) =>
        finding.Rule == suppression.Rule && finding.File == suppression.File.Path
        && (finding.Line == suppression.Line || finding.Line == suppression.Line + 1);

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
            Field(Approver, match)));
    }

    private static string Field(Regex field, Match marker) =>
        field.Match(marker.Groups["rest"].Value).Groups["value"].Value;
}
