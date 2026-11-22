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
            .Where(declared => IsNonPublic(declared.Symbol) && !IsExempt(declared.Symbol) && !referenced.Contains(SymbolIds.Of(declared.Symbol)))
            .Select(declared => Findings.OnSymbol("DC01", declared.File, declared.Symbol, Message(declared.Symbol)));
    }

    private static string Message(ISymbol symbol) =>
        $"DC01: {symbol.DeclaredAccessibility.ToString().ToLowerInvariant()} symbol {symbol.Name} has 0 references in production code, minimum 1. "
        + "Delete it, or use it from production code.";

    private static IEnumerable<ISymbol> Declared(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes()
            .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or MethodDeclarationSyntax or PropertyDeclarationSyntax
                or VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax })
            .Select(node => file.Model.GetDeclaredSymbol(node)).OfType<ISymbol>();

    private static bool IsNonPublic(ISymbol symbol) =>
        symbol.DeclaredAccessibility is Accessibility.Private or Accessibility.Internal;

    private static bool IsExempt(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method => method.IsOverride || method.ExplicitInterfaceImplementations.Any() || IsEntryPoint(method),
        IPropertySymbol property => property.IsOverride || property.ExplicitInterfaceImplementations.Any(),
        INamedTypeSymbol type => type.GetMembers("Main").OfType<IMethodSymbol>().Any(IsEntryPoint),
        _ => false,
    };

    private static bool IsEntryPoint(IMethodSymbol method) => method is { Name: "Main", IsStatic: true };

    private static IEnumerable<string> References(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>()
            .Select(name => (Site: name, Target: Symbols.Lookup(file.Model, name)))
            .Where(reference => reference.Target is not null && !IsInsideOwnDeclaration(reference.Site, reference.Target))
            .SelectMany(reference => WithContainingTypes(Symbols.Definition(reference.Target!)))
            .Select(SymbolIds.Of);

    private static bool IsInsideOwnDeclaration(SyntaxNode site, ISymbol target) =>
        target.DeclaringSyntaxReferences.Any(declaration => declaration.SyntaxTree == site.SyntaxTree && declaration.Span.Contains(site.Span));

    private static IEnumerable<ISymbol> WithContainingTypes(ISymbol symbol) =>
        symbol.ContainingType is { } outer ? WithContainingTypes(outer.OriginalDefinition).Append(symbol) : [symbol];
}
