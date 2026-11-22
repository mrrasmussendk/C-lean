using System.Linq;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Analysis;

/// <summary>One-based line numbers of syntax.</summary>
internal static class Lines
{
    public static int Of(SyntaxToken token) => StartOf(token.GetLocation());

    public static int Of(SyntaxNode node) => StartOf(node.GetLocation());

    public static int StartOf(Location location) => location.GetLineSpan().StartLinePosition.Line + 1;

    public static int EndOf(Location location) => location.GetLineSpan().EndLinePosition.Line + 1;

    public static int Of(ISymbol symbol, SyntaxTree tree) =>
        symbol.Locations.Where(location => location.SourceTree == tree).Select(StartOf).DefaultIfEmpty(1).First();
}
