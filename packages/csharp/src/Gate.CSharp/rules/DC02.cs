// DC02: Comments must not contain code
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class DC02
{
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings)
    {
        var comments = file.Tree.GetRoot().DescendantTrivia().Where(IsPlainComment).ToList();
        var lines = comments.Select(comment => Lines.StartOf(comment.GetLocation())).ToList();
        return Enumerable.Range(0, comments.Count)
            .Where(index => index == 0 || !Continues(comments[index], lines[index] - lines[index - 1]))
            .SelectMany(
                start =>
                    CodeLines(
                        comments.Skip(start)
                            .TakeWhile(
                                (comment, offset) =>
                                    offset == 0
                                        || Continues(comment, lines[start + offset] - lines[start + offset - 1]))
                            .ToList()))
            .Select(
                line =>
                    Findings.OnLine(
                        "DC02",
                        file,
                        line,
                        "DC02: this comment contains code. Delete it; version control keeps the old code."));
    }

    private static bool IsPlainComment(SyntaxTrivia trivia) =>
        trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);

    private static bool Continues(SyntaxTrivia comment, int linesAfterPrevious) =>
        comment.IsKind(SyntaxKind.SingleLineCommentTrivia) && linesAfterPrevious == 1;

    /// <summary>The first line of a run of comments when the run is code as a whole, else each line that is code
    /// alone.</summary>
    private static IEnumerable<int> CodeLines(IReadOnlyList<SyntaxTrivia> run)
    {
        var texts = run.Select(Text).ToList();
        if (IsCode(string.Join("\n", texts)))
        {
            return [Lines.StartOf(run[0].GetLocation())];
        }
        return run.Where((comment, index) => IsCode(texts[index]))
            .Select(comment => Lines.StartOf(comment.GetLocation()));
    }

    private static string Text(SyntaxTrivia comment)
    {
        var text = comment.ToString();
        return comment.IsKind(SyntaxKind.SingleLineCommentTrivia)
            ? text.TrimStart('/')
            : string.Join(
                "\n",
                text.TrimStart('/')
                    .Trim('*')
                    .TrimEnd('/')
                    .Trim('*')
                    .Split('\n')
                    .Select(line => line.Trim().TrimStart('*')));
    }

    private static bool IsCode(string text) =>
        Parses(
            $"class C {{ void M() {{ {text}\n }} }}",
            node =>
                node is BlockSyntax { Parent: MethodDeclarationSyntax } body && body.Statements.Any(IsCodeStatement))
        || Parses(
            $"class C {{ {text}\n }}",
            node =>
                node is MemberDeclarationSyntax { Parent.Parent: CompilationUnitSyntax } and not IncompleteMemberSyntax)
        || Parses(text, node => node is UsingDirectiveSyntax);

    private static bool Parses(string source, System.Func<SyntaxNode, bool> isCode)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return !tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            && tree.GetRoot().DescendantNodes().Any(isCode);
    }

    private static bool IsCodeStatement(StatementSyntax statement) => statement switch
    {
        ExpressionStatementSyntax { Expression: var expression } =>
            expression is AssignmentExpressionSyntax or InvocationExpressionSyntax or AwaitExpressionSyntax
                or PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax,
        LocalDeclarationStatementSyntax or LocalFunctionStatementSyntax or ReturnStatementSyntax
            or ThrowStatementSyntax =>
            true,
        IfStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
            or CommonForEachStatementSyntax =>
            true,
        SwitchStatementSyntax or TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax => true,
        _ => false,
    };
}
