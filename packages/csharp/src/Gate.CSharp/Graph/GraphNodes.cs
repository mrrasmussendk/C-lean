using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Graph;

/// <summary>A node per type, per abstraction (interface or abstract class) and per function declared in a
/// file.</summary>
internal static class GraphNodes
{
    public static IEnumerable<GraphNode> Of(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes().SelectMany(node => Node(file, node));

    private static IEnumerable<GraphNode> Node(SourceFile file, SyntaxNode node) => node switch
    {
        BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => TypeNode(file, node),
        MethodDeclarationSyntax => FunctionNode(file, node),
        _ when Functions.IsFunction(node) && Functions.Body(node) is not null => FunctionNode(file, node),
        _ => [],
    };

    private static IEnumerable<GraphNode> TypeNode(SourceFile file, SyntaxNode node)
    {
        if (file.Model.GetDeclaredSymbol(node) is not INamedTypeSymbol type)
        {
            return [];
        }
        var isAbstraction = type.TypeKind == TypeKind.Interface
            || type is { TypeKind: TypeKind.Class, IsAbstract: true, IsStatic: false };
        var kind = isAbstraction ? "abstraction" : "type";
        return [new GraphNode(SymbolIds.Of(type), kind, file.Component, file.Path, Lines.Of(type, file.Tree),
            file.IsTest, IsPublic(type), false, false)];
    }

    private static IEnumerable<GraphNode> FunctionNode(SourceFile file, SyntaxNode node)
    {
        var function = Functions.Symbol(file.Model, node);
        if (function is null)
        {
            return [];
        }
        var line = Lines.Of(Functions.NameToken(node));
        var isProperty = file.IsTest && TestCode.IsPropertyBased(node);
        return [new GraphNode(SymbolIds.Of(function), "function", file.Component, file.Path, line, file.IsTest,
            IsPublic(function), HasEffect(file, node), isProperty)];
    }

    private static bool IsPublic(ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Public;

    private static bool HasEffect(SourceFile file, SyntaxNode node)
    {
        var own = Functions.Body(node) is { } body ? Functions.OwnNodes(body).ToList() : [];
        return EffectApis.Uses(own, file.Model).Any()
            || StateWrites.In(own, file.Model).Any(StateWrites.IsMutableStatic);
    }
}
