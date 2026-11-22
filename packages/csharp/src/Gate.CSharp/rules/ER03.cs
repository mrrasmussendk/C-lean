// ER03: A function with side effects must not signal failure through a boolean or integer return value
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class ER03
{
    private static readonly SpecialType[] Integers =
    [
        SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64,
    ];

    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var own = Functions.OwnNodes(unit.Body).ToList();
        var hasEffects = EffectApis.Uses(own, unit.File.Model).Any() || StateWrites.In(own, unit.File.Model).Any();
        var signals = Returns.Of(unit).Where(value => IsFailureSignal(value, Returns.DeclaredType(unit.Symbol))).ToList();
        if (unit.Node is AnonymousFunctionExpressionSyntax || !hasEffects || signals.Count == 0)
        {
            return [];
        }
        var message = $"ER03: function {unit.Name} has side effects and signals failure by returning {signals[0]}. "
            + "Throw an exception or return a result type that says what failed.";
        return [Findings.OnFunction("ER03", unit, message)];
    }

    private static bool IsFailureSignal(ExpressionSyntax value, ITypeSymbol declared) => value switch
    {
        LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.FalseLiteralExpression) && declared.SpecialType == SpecialType.System_Boolean,
        PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax } negative
            => negative.IsKind(SyntaxKind.UnaryMinusExpression) && Integers.Contains(declared.SpecialType),
        _ => false,
    };
}
