// DC01: A non-public symbol must be referenced by production code
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class DC01
{
    public static IEnumerable<Finding> Check(Solution solution, Settings settings)
    {
        var referenced = solution.Files.SelectMany(References).ToHashSet();
        return solution.Files.SelectMany(file => Declared(file).Select(symbol => (File: file, Symbol: symbol)))
            .Where(
                declared =>
                    IsNonPublic(declared.Symbol)
                        && !IsExempt(declared.Symbol)
                        && !referenced.Contains(SymbolIds.Of(declared.Symbol)))
            .Select(declared => Findings.OnSymbol("DC01", declared.File, declared.Symbol, Message(declared.Symbol)));
    }

    private static string Message(ISymbol symbol) =>
        $"DC01: {symbol.DeclaredAccessibility.ToString().ToLowerInvariant()} symbol {symbol.Name} has 0 references "
            + $"in production code, minimum 1. "
        + "Delete it, or use it from production code.";

    private static IEnumerable<ISymbol> Declared(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes()
            .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or MethodDeclarationSyntax
                or PropertyDeclarationSyntax
                or VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax })
            .Select(node => file.Model.GetDeclaredSymbol(node)).OfType<ISymbol>();

    private static bool IsNonPublic(ISymbol symbol) =>
        symbol.DeclaredAccessibility is Accessibility.Private or Accessibility.Internal;

    private static bool IsExempt(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method =>
            method.IsOverride || method.ExplicitInterfaceImplementations.Any() || IsEntryPoint(method),
        IPropertySymbol property => property.IsOverride || property.ExplicitInterfaceImplementations.Any(),
        INamedTypeSymbol type => type.GetMembers("Main").OfType<IMethodSymbol>().Any(IsEntryPoint),
        _ => false,
    };

    private static bool IsEntryPoint(IMethodSymbol method) => method is { Name: "Main", IsStatic: true };

    private static IEnumerable<string> References(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes()
            .SelectMany(node => Targets(node, file.Model).Select(target => (Site: node, Target: target)))
            .Where(reference => !IsInsideOwnDeclaration(reference.Site, reference.Target))
            .SelectMany(reference => Credited(reference.Site, Symbols.Definition(reference.Target)))
            .Select(SymbolIds.Of);

    /// <summary>What a node refers to: the symbol a name stands for, and the members the language calls without
    /// naming them, for foreach, a collection initializer, a deconstruction and await.</summary>
    private static IEnumerable<ISymbol> Targets(SyntaxNode node, SemanticModel model) => node switch
    {
        SimpleNameSyntax name => new[] { Symbols.Lookup(model, name) }.OfType<ISymbol>(),
        CommonForEachStatementSyntax loop => Iteration(model.GetForEachStatementInfo(loop)),
        ExpressionSyntax { Parent: InitializerExpressionSyntax list } element
            when list.IsKind(SyntaxKind.CollectionInitializerExpression) =>
            new[] { model.GetCollectionInitializerSymbolInfo(element).Symbol }.OfType<ISymbol>(),
        AssignmentExpressionSyntax { Left: TupleExpressionSyntax or DeclarationExpressionSyntax } assignment =>
            new[] { model.GetDeconstructionInfo(assignment).Method }.OfType<ISymbol>(),
        AwaitExpressionSyntax awaited => new[] { model.GetAwaitExpressionInfo(awaited).GetAwaiterMethod }
            .OfType<ISymbol>(),
        _ => [],
    };

    private static IEnumerable<ISymbol> Iteration(ForEachStatementInfo loop) =>
        new ISymbol?[] { loop.GetEnumeratorMethod, loop.MoveNextMethod, loop.CurrentProperty, loop.DisposeMethod }
            .OfType<ISymbol>();

    /// <summary>A reference counts for the symbol and for the types that contain it, except a containing type the
    /// reference sits inside: a type that only its own members use is used by nobody.</summary>
    private static IEnumerable<ISymbol> Credited(SyntaxNode site, ISymbol symbol) =>
        WithContainingTypes(symbol).Where(
            credited => SymbolEqualityComparer.Default.Equals(credited, symbol)
                || !IsInsideOwnDeclaration(site, credited));

    private static bool IsInsideOwnDeclaration(SyntaxNode site, ISymbol target) =>
        target.DeclaringSyntaxReferences.Any(
            declaration => declaration.SyntaxTree == site.SyntaxTree && declaration.Span.Contains(site.Span));

    private static IEnumerable<ISymbol> WithContainingTypes(ISymbol symbol) =>
        symbol.ContainingType is { } outer ? WithContainingTypes(outer.OriginalDefinition).Append(symbol) : [symbol];
}
