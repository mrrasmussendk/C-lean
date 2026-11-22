using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Gate.CSharp.Analysis;

/// <summary>Symbol IDs: csharp:assembly:fully qualified name(parameter types).</summary>
internal static class SymbolIds
{
    private static readonly SymbolDisplayFormat Qualified = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public static string Of(ISymbol symbol) =>
        $"csharp:{symbol.ContainingAssembly?.Name ?? "-"}:{Name(symbol)}{Parameters(symbol)}";

    private static string Name(ISymbol symbol) => symbol switch
    {
        ITypeSymbol type => TypeName(type),
        IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } lambda => LambdaName(lambda),
        _ => $"{Name(symbol.ContainingSymbol)}.{symbol.MetadataName}",
    };

    private static string Parameters(ISymbol symbol) => symbol switch
    {
        IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } => "()",
        IMethodSymbol method => $"({string.Join(",", method.Parameters.Select(parameter => TypeName(parameter.Type)))})",
        _ => "",
    };

    private static string TypeName(ITypeSymbol type) => type.ToDisplayString(Qualified).Replace(" ", "");

    private static string LambdaName(IMethodSymbol lambda)
    {
        var owner = NamedOwner(lambda.ContainingSymbol);
        var site = lambda.DeclaringSyntaxReferences.Select(reference => reference.Span).FirstOrDefault();
        var index = LambdaSpans(owner).TakeWhile(span => span != site).Count() + 1;
        return $"{Name(owner)}.<lambda>{index}";
    }

    private static ISymbol NamedOwner(ISymbol symbol) =>
        symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } ? NamedOwner(symbol.ContainingSymbol) : symbol;

    private static IEnumerable<TextSpan> LambdaSpans(ISymbol owner) =>
        owner.DeclaringSyntaxReferences
            .SelectMany(reference => reference.GetSyntax().DescendantNodes())
            .OfType<AnonymousFunctionExpressionSyntax>()
            .Select(lambda => lambda.Span);
}
