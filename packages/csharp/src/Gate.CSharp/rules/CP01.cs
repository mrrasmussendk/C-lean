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
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings)
    {
        var limit = settings.Limits["CP01"];
        return file.Tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>()
            .Where(link => link is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
            .Select(
                link =>
                    (
                        Start: First(link),
                        Passed: Intermediates(link, file.Model).Count(step => IsBehavioural(step, file.Model))))
            .Where(chain => chain.Passed > limit)
            .GroupBy(chain => chain.Start.SpanStart)
            .Select(chains => chains.MaxBy(chain => chain.Passed))
            .Select(chain => Findings.OnLine("CP01", file, Lines.Of(chain.Start), Message(chain.Passed, limit)));
    }

    private static string Message(int passed, int limit) =>
        $"CP01: member-access chain passes through {passed} behavioural objects, limit {limit}. "
        + "Ask the first object for what you need instead of reaching through it.";

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

    private static ExpressionSyntax First(ExpressionSyntax link) =>
        Receiver(link) is { } receiver ? First(receiver) : link;

    private static IEnumerable<ExpressionSyntax> Intermediates(ExpressionSyntax link, SemanticModel model)
    {
        var receiver = Receiver(link);
        if (receiver is null || IsFirstReceiver(receiver, model))
        {
            return [];
        }
        return Intermediates(receiver, model).Append(receiver);
    }

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
