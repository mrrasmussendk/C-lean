using System.Linq;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Analysis;

/// <summary>Symbol lookups that tolerate unresolved references.</summary>
internal static class Symbols
{
    public static ISymbol? Lookup(SemanticModel model, SyntaxNode node)
    {
        var info = model.GetSymbolInfo(node);
        return info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
    }

    public static ISymbol Definition(ISymbol symbol) =>
        symbol is IMethodSymbol { ReducedFrom: { } full } ? full.OriginalDefinition : symbol.OriginalDefinition;

    public static bool IsInSource(ISymbol symbol) => symbol.Locations.Any(location => location.IsInSource);

    public static bool IsReceiver(IParameterSymbol parameter) =>
        parameter.ContainingSymbol is IMethodSymbol { IsExtensionMethod: true } && parameter.Ordinal == 0;
}
