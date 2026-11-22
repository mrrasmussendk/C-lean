using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>
/// Normalized hash of a syntax fragment: tokens in source order without trivia, each identifier
/// replaced by a placeholder numbered by first occurrence, each literal by a placeholder for its
/// type, keywords and operators kept; SHA-256 of the result.
/// </summary>
internal static class NormalizedHash
{
    private static readonly SyntaxKind[] TextKinds =
    [
        SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken,
            SyntaxKind.SingleLineRawStringLiteralToken,
        SyntaxKind.MultiLineRawStringLiteralToken, SyntaxKind.Utf8StringLiteralToken,
    ];

    public static string Of(IEnumerable<SyntaxNode> fragment)
    {
        var tokens = fragment.SelectMany(node => node.DescendantTokens()).ToList();
        var names = tokens.Where(IsIdentifier).Select(token => token.ValueText).Distinct().ToList();
        return Hashing.Sha256(string.Join(" ", tokens.Select(token => Normalized(token, names))));
    }

    private static bool IsIdentifier(SyntaxToken token) =>
        token.IsKind(SyntaxKind.IdentifierToken) && token.Parent is not IdentifierNameSyntax { IsVar: true };

    private static string Normalized(SyntaxToken token, List<string> names)
    {
        if (IsIdentifier(token))
        {
            return $"${names.IndexOf(token.ValueText)}";
        }
        return LiteralType(token) is { Length: > 0 } type ? $"#{type}" : token.Text;
    }

    private static string LiteralType(SyntaxToken token) => token.Kind() switch
    {
        SyntaxKind.NumericLiteralToken => NumberType(token.Value),
        SyntaxKind.CharacterLiteralToken => "char",
        SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword => "bool",
        var kind when TextKinds.Contains(kind) => "string",
        _ => "",
    };

    private static string NumberType(object? value) => value switch
    {
        int => "int",
        uint => "uint",
        long => "long",
        ulong => "ulong",
        float => "float",
        double => "double",
        decimal => "decimal",
        _ => "number",
    };
}
