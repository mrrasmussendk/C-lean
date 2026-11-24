using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>Symbol lookups that tolerate unresolved references.</summary>
internal static class Symbols
{
    /// <summary>
    /// The symbol a node names. The name of a called function is answered from the call: the semantic model keeps
    /// calls but not their method groups, and binds each group again with the whole chain it is called on.
    /// </summary>
    public static ISymbol? Lookup(SemanticModel model, SyntaxNode node) =>
        CallOf(node) is { } call
            && Resolve(model, call) is IMethodSymbol { MethodKind: not MethodKind.DelegateInvoke } called
            ? called
            : Resolve(model, node);

    private static ISymbol? Resolve(SemanticModel model, SyntaxNode node)
    {
        var info = model.GetSymbolInfo(node);
        return info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
    }

    /// <summary>The call whose function this node names: the callee itself, or the name on its right side.</summary>
    private static InvocationExpressionSyntax? CallOf(SyntaxNode node)
    {
        var callee = node.Parent switch
        {
            MemberAccessExpressionSyntax access when access.Name == node => access,
            MemberBindingExpressionSyntax binding when binding.Name == node => binding,
            _ => node,
        };
        return callee.Parent is InvocationExpressionSyntax call && call.Expression == callee ? call : null;
    }

    public static ISymbol Definition(ISymbol symbol) =>
        symbol is IMethodSymbol { ReducedFrom: { } full } ? full.OriginalDefinition : symbol.OriginalDefinition;

    public static bool IsInSource(ISymbol symbol) => symbol.Locations.Any(location => location.IsInSource);

    /// <summary>The primary constructor of a type, declared by the parameter list after the type's name.</summary>
    public static IMethodSymbol? PrimaryConstructor(INamedTypeSymbol type) =>
        type.InstanceConstructors.FirstOrDefault(
            constructor => constructor.DeclaringSyntaxReferences.Any(
                reference => reference.GetSyntax() is TypeDeclarationSyntax));

    public static bool IsOfPrimaryConstructor(IParameterSymbol parameter) =>
        parameter.ContainingSymbol is IMethodSymbol { ContainingType: { } type } constructor
        && SymbolEqualityComparer.Default.Equals(PrimaryConstructor(type), constructor);

    public static bool IsReceiver(IParameterSymbol parameter) =>
        parameter.ContainingSymbol is IMethodSymbol { IsExtensionMethod: true } && parameter.Ordinal == 0;
}
