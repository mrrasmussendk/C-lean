using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Graph;

/// <summary>
/// Edges read from the semantic model. The source of an edge is the innermost function that
/// holds the reference, or the type when the reference is in a field or property declaration.
/// </summary>
internal static class GraphEdges
{
    private const string Call = "call";
    private const string Construct = "construct";
    private const string Inherit = "inherit";
    private const string TypeReference = "type_reference";

    private sealed record Target(ISymbol Symbol, string Kind);

    public static IEnumerable<GraphEdge> Of(SourceFile file)
    {
        var nodes = file.Tree.GetRoot().DescendantNodesAndSelf().ToList();
        var owners = nodes.Select(node => (Node: node, Symbol: Declared(file.Model, node)))
            .Where(owner => owner.Symbol is not null)
            .ToDictionary(owner => owner.Node, owner => SymbolIds.Of(owner.Symbol!));
        var around = Around(nodes, owners);
        var references = nodes.SelectMany(node => Targets(file.Model, node)
            .Select(
                target =>
                    new GraphEdge(
                        around[node],
                        SymbolIds.Of(Symbols.Definition(target.Symbol)),
                        target.Kind)));
        var inherits = owners.Keys.Select(node => file.Model.GetDeclaredSymbol(node)).OfType<INamedTypeSymbol>()
            .SelectMany(type => new[] { type.BaseType }.Concat(type.Interfaces).OfType<INamedTypeSymbol>()
                .Select(parent => new GraphEdge(SymbolIds.Of(type), SymbolIds.Of(parent.OriginalDefinition), Inherit)));
        return references.Concat(inherits);
    }

    private static ISymbol? Declared(SemanticModel model, SyntaxNode node) => node switch
    {
        BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => model.GetDeclaredSymbol(node),
        _ when Functions.IsFunction(node) => Functions.Symbol(model, node),
        _ => null,
    };

    /// <summary>The innermost owner around every node; empty where there is none. The nodes come parents first, so
    /// each one takes its own ID or the answer of its parent, without climbing the tree again.</summary>
    private static Dictionary<SyntaxNode, string> Around(
        IReadOnlyList<SyntaxNode> nodes,
        IReadOnlyDictionary<SyntaxNode, string> owners)
    {
        var around = new Dictionary<SyntaxNode, string>();
        foreach (var node in nodes)
        {
            var outer = node.Parent is null ? "" : around.GetValueOrDefault(node.Parent, "");
            around[node] = owners.GetValueOrDefault(node, outer);
        }
        return around;
    }

    private static IEnumerable<Target> Targets(SemanticModel model, SyntaxNode node) => node switch
    {
        ImplicitObjectCreationExpressionSyntax => Typed(model.GetTypeInfo(node).Type, Construct),
        IdentifierNameSyntax { IsVar: true } => [],
        SimpleNameSyntax name => Named(model, name),
        _ => [],
    };

    private static IEnumerable<Target> Typed(ISymbol? type, string kind) =>
        type is null ? [] : [new Target(type, kind)];

    private static IEnumerable<Target> Named(SemanticModel model, SimpleNameSyntax name) =>
        Symbols.Lookup(model, name) switch
    {
        IMethodSymbol { MethodKind: MethodKind.Constructor } => [],
        IMethodSymbol function => [new Target(function, Call)],
        IPropertySymbol property => Typed(IsAssigned(name) ? property.SetMethod : property.GetMethod, Call),
        INamedTypeSymbol type => TypeUse(model, name).Select(kind => new Target(type, kind)),
        _ => [],
    };

    /// <summary>How a type name is used: constructed, referenced, or nothing when another edge already says
    /// it.</summary>
    private static IEnumerable<string> TypeUse(SemanticModel model, SimpleNameSyntax name)
    {
        var whole = Whole(name);
        if (whole.Parent is ObjectCreationExpressionSyntax)
        {
            return [Construct];
        }
        var isBase = whole.Parent is BaseTypeSyntax;
        var qualifiesCall = whole.Parent is MemberAccessExpressionSyntax access && access.Expression == whole
            && Symbols.Lookup(model, access.Name) is IMethodSymbol;
        return isBase || qualifiesCall ? [] : [TypeReference];
    }

    private static SyntaxNode Whole(SyntaxNode name) =>
        name.Parent is QualifiedNameSyntax qualified && qualified.Right == name ? Whole(qualified) : name;

    private static bool IsAssigned(SimpleNameSyntax name)
    {
        SyntaxNode written = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access : name;
        return written.Parent is AssignmentExpressionSyntax assignment && assignment.Left == written;
    }
}
