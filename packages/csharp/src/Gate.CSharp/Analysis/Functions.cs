using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Analysis;

/// <summary>
/// A function is any named or anonymous callable with a body. The top-level statements of a file are one
/// function: its node and its body are the compilation unit, and its parts are the statements.
/// </summary>
internal static class Functions
{
    public const string TopLevelName = "<top-level>";

    public static IEnumerable<FunctionUnit> Of(SourceFile file) =>
        file.Tree.GetRoot().DescendantNodesAndSelf().Where(IsFunction).SelectMany(node => Unit(file, node));

    public static bool IsFunction(SyntaxNode node) =>
        node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax
            or AccessorDeclarationSyntax or ArrowExpressionClauseSyntax { Parent: BasePropertyDeclarationSyntax }
        || TopLevelStatements(node).Any();

    public static SyntaxNode? Body(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody?.Expression,
        AccessorDeclarationSyntax accessor => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody?.Expression,
        AnonymousFunctionExpressionSyntax lambda => lambda.Body,
        ArrowExpressionClauseSyntax arrow => arrow.Expression,
        CompilationUnitSyntax when TopLevelStatements(node).Any() => node,
        _ => null,
    };

    /// <summary>The pieces of a body that are measured: the body itself, or each top-level statement.</summary>
    public static IEnumerable<SyntaxNode> Parts(SyntaxNode body) =>
        body is CompilationUnitSyntax ? TopLevelStatements(body) : [body];

    public static IMethodSymbol? Symbol(SemanticModel model, SyntaxNode node) => node switch
    {
        AnonymousFunctionExpressionSyntax => model.GetSymbolInfo(node).Symbol as IMethodSymbol,
        ArrowExpressionClauseSyntax { Parent: { } property } =>
            (model.GetDeclaredSymbol(property) as IPropertySymbol)?.GetMethod,
        CompilationUnitSyntax unit => model.GetDeclaredSymbol(unit),
        _ => model.GetDeclaredSymbol(node) as IMethodSymbol,
    };

    /// <summary>The token a function is reported on: its name, or the keyword that stands for one.</summary>
    public static SyntaxToken NameToken(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax method => method.Identifier,
        ConstructorDeclarationSyntax constructor => constructor.Identifier,
        DestructorDeclarationSyntax destructor => destructor.Identifier,
        OperatorDeclarationSyntax operation => operation.OperatorToken,
        ConversionOperatorDeclarationSyntax conversion => conversion.OperatorKeyword,
        LocalFunctionStatementSyntax local => local.Identifier,
        AccessorDeclarationSyntax accessor => accessor.Keyword,
        ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax property } => property.Identifier,
        ArrowExpressionClauseSyntax { Parent: IndexerDeclarationSyntax indexer } => indexer.ThisKeyword,
        CompilationUnitSyntax => TopLevelStatements(node).Select(statement => statement.GetFirstToken()).First(),
        _ => node.GetFirstToken(),
    };

    /// <summary>The nodes of a body that belong to the function itself: nested functions are listed but not
    /// entered, and the types declared beside top-level statements are left out.</summary>
    public static IEnumerable<SyntaxNode> OwnNodes(SyntaxNode body) =>
        body.DescendantNodesAndSelf(node => node == body || !(IsFunction(node) || IsDeclaration(node)))
            .Where(node => !IsDeclaration(node));

    private static bool IsDeclaration(SyntaxNode node) =>
        node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or BaseNamespaceDeclarationSyntax;

    private static IEnumerable<GlobalStatementSyntax> TopLevelStatements(SyntaxNode node) =>
        node is CompilationUnitSyntax unit ? unit.Members.OfType<GlobalStatementSyntax>() : [];

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

    private static string DisplayName(IMethodSymbol symbol) => symbol switch
    {
        { MethodKind: MethodKind.AnonymousFunction } => "<lambda>",
        { Name: WellKnownMemberNames.TopLevelStatementsEntryPointMethodName } => TopLevelName,
        _ => symbol.Name,
    };
}
