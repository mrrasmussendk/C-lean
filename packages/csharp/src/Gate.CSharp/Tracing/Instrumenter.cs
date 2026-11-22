using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Tracing;

/// <summary>
/// Rewrites a source file so that every function with a body enters the component of its file
/// and restores the previous one on exit. The rewrite is a list of text edits: a try/finally
/// around a block body, a block in place of an expression body, and a pause around each
/// `yield return` so that the consumer of an iterator runs under its own component.
/// Not rewritten: expression-bodied lambdas (they may be expression trees) and expression
/// bodies whose function symbol does not resolve.
/// </summary>
internal static class Instrumenter
{
    /// <summary>The texts inserted for one function: enter and open the try, close it and exit, and the same around a
    /// yield.</summary>
    private sealed record Probe(string Open, string Close, string Pause, string Resume);

    public static string Rewrite(SourceFile file, string helper)
    {
        var root = file.Tree.GetRoot();
        var functions = root.DescendantNodes().Where(Functions.IsFunction)
            .SelectMany(node => FunctionEdits(file, node, ProbeFor(node.SpanStart, file.Component, helper)));
        var topLevel = TopLevelEdits(root, ProbeFor(0, file.Component, helper));
        return TextEdits.Apply(file.Tree.GetText().ToString(), topLevel.Concat(functions));
    }

    private static Probe ProbeFor(int position, string component, string helper)
    {
        var slot = $"__gate{position}";
        var enter = $"global::{helper}.Enter({SymbolDisplay.FormatLiteral(component, true)})";
        var exit = $"global::{helper}.Exit({slot});";
        return new Probe(
            $"var {slot} = {enter}; try {{",
            $"}} finally {{ {exit} }}",
            $"{{ {exit} ",
            $" {slot} = {enter}; }}");
    }

    private static IEnumerable<Edit> TopLevelEdits(SyntaxNode root, Probe probe)
    {
        var statements = root.ChildNodes().OfType<GlobalStatementSyntax>().ToList();
        return statements.Take(1).Select(first => TextEdits.Insert(first.SpanStart, $"{probe.Open} "))
            .Concat(statements.TakeLast(1).Select(last => TextEdits.Insert(last.Span.End, $" {probe.Close}")));
    }

    private static IEnumerable<Edit> FunctionEdits(SourceFile file, SyntaxNode node, Probe probe) => node switch
    {
        ArrowExpressionClauseSyntax arrow => ArrowEdits(file, arrow, probe),
        AnonymousFunctionExpressionSyntax lambda => BlockEdits(lambda.Block, probe),
        _ when Functions.Body(node) is BlockSyntax block => BlockEdits(block, probe),
        _ =>
            node.ChildNodes().OfType<ArrowExpressionClauseSyntax>().SelectMany(arrow => ArrowEdits(file, arrow, probe)),
    };

    private static IEnumerable<Edit> BlockEdits(BlockSyntax? block, Probe probe)
    {
        if (block is null)
        {
            return [];
        }
        var yields = Functions.OwnNodes(block).OfType<YieldStatementSyntax>()
            .Where(statement => statement.IsKind(SyntaxKind.YieldReturnStatement))
            .SelectMany(
                statement =>
                    new[] { TextEdits.Insert(statement.SpanStart, probe.Pause), TextEdits.Insert(statement.Span.End,
                        probe.Resume) });
        var open = TextEdits.Insert(block.OpenBraceToken.Span.End, $" {probe.Open}");
        return new[] { open }.Concat(yields)
            .Append(TextEdits.Insert(block.CloseBraceToken.SpanStart, $"{probe.Close} "));
    }

    /// <summary>`=> value;` becomes a block that returns the value; on a property it becomes a get accessor.</summary>
    private static IEnumerable<Edit> ArrowEdits(SourceFile file, ArrowExpressionClauseSyntax arrow, Probe probe)
    {
        var owner = arrow.Parent is BasePropertyDeclarationSyntax ? arrow : arrow.Parent;
        var function = owner is null ? null : Functions.Symbol(file.Model, owner);
        var semicolon = arrow.Parent?.ChildTokens()
            .LastOrDefault(token => token.IsKind(SyntaxKind.SemicolonToken)) ?? default;
        if (function is null || semicolon.IsKind(SyntaxKind.None))
        {
            return [];
        }
        var (lead, trail) = arrow.Parent is BasePropertyDeclarationSyntax ? ("{ get ", " }") : ("", "");
        var keyword = IsStatement(function, arrow.Expression) ? "" : "return ";
        return
        [
            new Edit(arrow.ArrowToken.SpanStart, arrow.ArrowToken.Span.End, $"{lead}{{ {probe.Open} {keyword}"),
            TextEdits.Insert(arrow.Expression.Span.End, $"; {probe.Close} }}{trail}"),
            new Edit(semicolon.SpanStart, semicolon.Span.End, ""),
        ];
    }

    /// <summary>An expression body that yields no value: a throw, a void function, or an async function without a
    /// result.</summary>
    private static bool IsStatement(IMethodSymbol function, ExpressionSyntax body) =>
        body is ThrowExpressionSyntax || function.ReturnsVoid
        || (function.IsAsync && function.ReturnType is not INamedTypeSymbol { IsGenericType: true });
}
