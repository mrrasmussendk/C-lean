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
        INamespaceSymbol or IModuleSymbol or IAssemblySymbol or { ContainingSymbol: null } => symbol.ToDisplayString(),
        IMethodSymbol { Name: WellKnownMemberNames.TopLevelStatementsEntryPointMethodName } => Functions.TopLevelName,
        IMethodSymbol { MethodKind: MethodKind.LocalFunction } local =>
            $"{Name(local.ContainingSymbol)}.{local.MetadataName}{Position(local)}",
        IMethodSymbol { Arity: > 0 } generic => $"{Name(generic.ContainingSymbol)}.{generic.MetadataName}`{generic.Arity}",
        _ => $"{Name(symbol.ContainingSymbol)}.{symbol.MetadataName}",
    };

    /// <summary>Two local functions of one function may share a name when they are in different blocks: the second
    /// and later ones carry their position among them, `#2`, `#3`.</summary>
    private static string Position(IMethodSymbol local)
    {
        var start = local.DeclaringSyntaxReferences.Select(reference => reference.Span.Start).FirstOrDefault();
        var earlier = local.ContainingSymbol.DeclaringSyntaxReferences
            .SelectMany(reference => reference.GetSyntax().DescendantNodes())
            .OfType<LocalFunctionStatementSyntax>()
            .Count(other => other.Identifier.ValueText == local.Name && other.SpanStart < start);
        return earlier == 0 ? "" : $"#{earlier + 1}";
    }

    private static string Parameters(ISymbol symbol) => symbol switch
    {
        IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } => "()",
        IMethodSymbol { Name: WellKnownMemberNames.TopLevelStatementsEntryPointMethodName } => "()",
        IMethodSymbol method =>
            $"({string.Join(",", method.Parameters.Select(parameter => TypeName(parameter.Type)))})",
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
        symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction }
            ? NamedOwner(symbol.ContainingSymbol) : symbol;

    private static IEnumerable<TextSpan> LambdaSpans(ISymbol owner) =>
        owner.DeclaringSyntaxReferences
            .SelectMany(reference => reference.GetSyntax().DescendantNodes())
            .OfType<AnonymousFunctionExpressionSyntax>()
            .Select(lambda => lambda.Span);
}
