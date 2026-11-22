// FR02: Pass-through: a function whose body is one call that forwards at least 80% of its parameters
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class FR02
{
    private const int Percent = 100;

    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var parameters = unit.Symbol.Parameters.Select(parameter => parameter.Name).ToList();
        var forwarded = OnlyCall(unit).Where(call => !IsEffectWrapper(call, unit.File)).SelectMany(call => call.ArgumentList.Arguments)
            .Select(argument => argument.Expression).OfType<IdentifierNameSyntax>()
            .Select(name => name.Identifier.ValueText).Intersect(parameters).Count();
        var limit = settings.Limits["FR02"];
        if (forwarded == 0 || forwarded * Percent < limit * parameters.Count)
        {
            return [];
        }
        var message = $"FR02: function {unit.Name} is one call that forwards {forwarded} of its {parameters.Count} parameters unchanged, "
            + $"limit {limit}%. Call the target directly, or give this layer behaviour of its own.";
        return [Findings.OnFunction("FR02", unit, message)];
    }

    /// <summary>Exempt: in an effect component, the one call is an effect API (the wrapper FX01 asks for).</summary>
    private static bool IsEffectWrapper(InvocationExpressionSyntax call, SourceFile file) =>
        file.IsEffect && EffectApis.Uses(call.Expression.DescendantNodesAndSelf(), file.Model).Any();

    private static IEnumerable<InvocationExpressionSyntax> OnlyCall(FunctionUnit unit)
    {
        var isNamed = unit.Node is MethodDeclarationSyntax or LocalFunctionStatementSyntax;
        var statements = unit.Body is BlockSyntax block ? block.Statements.Cast<SyntaxNode>().ToList() : [unit.Body];
        return isNamed && statements.Count == 1 ? Call(statements[0]) : [];
    }

    private static IEnumerable<InvocationExpressionSyntax> Call(SyntaxNode node) => node switch
    {
        InvocationExpressionSyntax call => [call],
        ExpressionStatementSyntax statement => Call(statement.Expression),
        ReturnStatementSyntax { Expression: { } value } => Call(value),
        AwaitExpressionSyntax awaited => Call(awaited.Expression),
        _ => [],
    };
}
