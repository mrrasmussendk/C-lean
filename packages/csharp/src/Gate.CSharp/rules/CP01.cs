// CP01: Law of Demeter: a member-access chain must not pass through more than one behavioural object defined in the solution. Exempt: data records, standard-library types, fluent calls returning the receiver's type
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class CP01
{
    /// <summary>A link of a chain: the expression the chain starts with and the behavioural objects passed on the way
    /// to the link.</summary>
    private sealed record Chain(ExpressionSyntax Start, int Passed);

    public static IEnumerable<Finding> Check(SourceFile file, Settings settings)
    {
        var limit = settings.Limits["CP01"];
        var steps = file.Tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(IsStep);
        return Chains(steps, file.Model).Where(chain => chain.Key is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
            .Select(chain => chain.Value)
            .Where(chain => chain.Passed > limit)
            .GroupBy(chain => chain.Start.SpanStart)
            .Select(same => same.MaxBy(chain => chain.Passed))
            .OfType<Chain>()
            .Select(chain => Findings.OnLine("CP01", file, Lines.Of(chain.Start), Message(chain.Passed, limit)));
    }

    private static string Message(int passed, int limit) =>
        $"CP01: member-access chain passes through {passed} behavioural objects, limit {limit}. "
        + "Ask the first object for what you need instead of reaching through it.";

    private static bool IsStep(ExpressionSyntax expression) =>
        expression is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax or ElementAccessExpressionSyntax
            or InvocationExpressionSyntax;

    /// <summary>
    /// Every step with its chain. A receiver ends before the step that is accessed on it, so going through the steps
    /// in the order they end meets each receiver first, and each step is one look at its receiver: a chain of a
    /// thousand calls costs a thousand looks.
    /// </summary>
    private static Dictionary<SyntaxNode, Chain> Chains(IEnumerable<ExpressionSyntax> steps, SemanticModel model)
    {
        var chains = new Dictionary<SyntaxNode, Chain>();
        foreach (var step in steps.OrderBy(step => step.Span.End).ThenByDescending(step => step.SpanStart))
        {
            var receiver = Receiver(step);
            var start = new Chain(receiver ?? step, 0);
            var before = receiver is null ? start : chains.GetValueOrDefault(receiver, start);
            var isPassed = receiver is not null && !IsFirstReceiver(receiver, model) && IsBehavioural(receiver, model);
            chains[step] = before with { Passed = before.Passed + (isPassed ? 1 : 0) };
        }
        return chains;
    }

    /// <summary>The expression a link is accessed on; null when the link starts the chain.</summary>
    private static ExpressionSyntax? Receiver(ExpressionSyntax link) => link switch
    {
        MemberAccessExpressionSyntax member => Bare(member.Expression),
        ElementAccessExpressionSyntax element => Bare(element.Expression),
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax or MemberBindingExpressionSyntax } call =>
            Receiver(call.Expression),
        MemberBindingExpressionSyntax binding => Bare(binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>()
            .FirstOrDefault(access => access.WhenNotNull.Span.Contains(binding.Span))?.Expression),
        _ => null,
    };

    private static ExpressionSyntax? Bare(ExpressionSyntax? expression) =>
        expression is PostfixUnaryExpressionSyntax forgiven ? Bare(forgiven.Operand) : expression;

    private static bool IsFirstReceiver(ExpressionSyntax expression, SemanticModel model) => Receiver(expression) switch
    {
        null or ThisExpressionSyntax or BaseExpressionSyntax => true,
        var receiver => Symbols.Lookup(model, receiver) is INamespaceOrTypeSymbol,
    };

    private static bool IsBehavioural(ExpressionSyntax step, SemanticModel model)
    {
        var type = model.GetTypeInfo(step).Type;
        var isFluent = step is InvocationExpressionSyntax && Receiver(step) is { } receiver
            && SymbolEqualityComparer.Default.Equals(type, model.GetTypeInfo(receiver).Type);
        return type is not null && !isFluent && Symbols.IsInSource(type)
            && type.GetMembers()
                .OfType<IMethodSymbol>()
                .Any(method => method is { MethodKind: MethodKind.Ordinary, IsImplicitlyDeclared: false });
    }
}
