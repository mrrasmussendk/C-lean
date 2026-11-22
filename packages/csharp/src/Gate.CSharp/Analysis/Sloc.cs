using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>Source lines of code: lines holding at least one token. The braces of a block body are not
/// counted.</summary>
internal static class Sloc
{
    public static int Of(SyntaxNode node) =>
        node.DescendantTokens().Where(token => !IsOwnBrace(token, node)).SelectMany(TokenLines).Distinct().Count();

    private static bool IsOwnBrace(SyntaxToken token, SyntaxNode node) => node is BlockSyntax && token.Parent == node;

    private static IEnumerable<int> TokenLines(SyntaxToken token)
    {
        var first = Lines.StartOf(token.GetLocation());
        return Enumerable.Range(first, Lines.EndOf(token.GetLocation()) - first + 1);
    }
}
