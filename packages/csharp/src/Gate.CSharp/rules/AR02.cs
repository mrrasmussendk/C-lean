// AR02: Output parameter: a function must not return data by writing through a parameter. Exempt: the language's standard try-parse pattern
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class AR02
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var written = Mutations.In(unit.Body, unit.File.Model).Select(mutation => mutation.Target).ToList();
        return unit.Symbol.Parameters
            .Where(
                parameter =>
                    IsDeclaredOutput(parameter, unit.Symbol)
                        || (ReturnsNoData(unit.Symbol) && IsIn(written, parameter)))
            .Select(parameter => Findings.OnSymbol("AR02", unit.File, parameter, Message(parameter, unit)));
    }

    private static string Message(IParameterSymbol parameter, FunctionUnit unit) =>
        $"AR02: function {unit.Name} returns data through parameter {parameter.Name}. "
        + "Return the data as the function's result, in a tuple or record when there are several values.";

    private static bool IsDeclaredOutput(IParameterSymbol parameter, IMethodSymbol function) => parameter.RefKind switch
    {
        RefKind.Ref => true,
        RefKind.Out => !IsTryParse(function),
        _ => false,
    };

    private static bool IsTryParse(IMethodSymbol function) =>
        function.Name.StartsWith("Try") && function.ReturnType.SpecialType == SpecialType.System_Boolean;

    private static bool ReturnsNoData(IMethodSymbol function) =>
        !Returns.HasValue(function) || Returns.DeclaredType(function).SpecialType == SpecialType.System_Boolean;

    private static bool IsIn(IReadOnlyList<IParameterSymbol> written, IParameterSymbol parameter) =>
        written.Any(target => SymbolEqualityComparer.Default.Equals(target, parameter));
}
