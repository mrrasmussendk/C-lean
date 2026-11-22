using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>A function is any named or anonymous callable with a body.</summary>
internal static class Functions
{
    public static IEnumerable<FunctionUnit> Of(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodes().Where(IsFunction).SelectMany(node => Unit(file, node));

    public static bool IsFunction(SyntaxNode node) =>
        node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax
            or AccessorDeclarationSyntax or ArrowExpressionClauseSyntax { Parent: BasePropertyDeclarationSyntax };

    public static SyntaxNode? Body(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody?.Expression,
        AccessorDeclarationSyntax accessor => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody?.Expression,
        AnonymousFunctionExpressionSyntax lambda => lambda.Body,
        ArrowExpressionClauseSyntax arrow => arrow.Expression,
        _ => null,
    };

    public static IMethodSymbol? Symbol(SemanticModel model, SyntaxNode node) => node switch
    {
        AnonymousFunctionExpressionSyntax => model.GetSymbolInfo(node).Symbol as IMethodSymbol,
        ArrowExpressionClauseSyntax { Parent: { } property } => (model.GetDeclaredSymbol(property) as IPropertySymbol)?.GetMethod,
        _ => model.GetDeclaredSymbol(node) as IMethodSymbol,
    };

    public static SyntaxToken NameToken(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax method => method.Identifier,
        ConstructorDeclarationSyntax constructor => constructor.Identifier,
        DestructorDeclarationSyntax destructor => destructor.Identifier,
        OperatorDeclarationSyntax operation => operation.OperatorToken,
        LocalFunctionStatementSyntax local => local.Identifier,
        AccessorDeclarationSyntax accessor => accessor.Keyword,
        ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax property } => property.Identifier,
        _ => node.GetFirstToken(),
    };

    /// <summary>The nodes of a body that belong to the function itself: nested functions are listed but not entered.</summary>
    public static IEnumerable<SyntaxNode> OwnNodes(SyntaxNode body) =>
        body.DescendantNodesAndSelf(node => node == body || !IsFunction(node));

    private static IEnumerable<FunctionUnit> Unit(SourceFile file, SyntaxNode node)
    {
        var body = Body(node);
        var symbol = Symbol(file.Model, node);
        if (body is null || symbol is null)
        {
            return [];
        }
        var line = Lines.Of(NameToken(node));
        return [new FunctionUnit(file, node, body, symbol, SymbolIds.Of(symbol), DisplayName(symbol), line)];
    }

    private static string DisplayName(IMethodSymbol symbol) =>
        symbol.MethodKind == MethodKind.AnonymousFunction ? "<lambda>" : symbol.Name;
}
