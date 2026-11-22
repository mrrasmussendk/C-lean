// DS02: The same group of parameters must not travel through more functions than the limit
using System;
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Rules;

internal static class DS02
{
    private const int GroupSize = 3;

    /// <summary>A function with its parameters as "type name" texts. Overrides and implementations share one
    /// Root.</summary>
    private sealed record Signature(FunctionUnit Function, string Root, IReadOnlySet<string> Parameters);

    public static IEnumerable<Finding> Check(Solution solution, Settings settings)
    {
        var limit = settings.Limits["DS02"];
        var signatures = solution.Units.Where(IsNamed).Select(SignatureOf)
            .Where(signature => signature.Parameters.Count >= GroupSize)
            .GroupBy(signature => signature.Root).Select(same => same.First()).ToList();
        var groups = Shared(signatures).Where(group => Carriers(group, signatures).Count > limit).ToList();
        return groups.Where(group => !groups.Any(larger => larger.Count > group.Count && larger.IsSupersetOf(group)))
            .Select(
                group =>
                    Finding(
                        group,
                        Functions.InOrder(Carriers(group, signatures).Select(carrier => carrier.Function)),
                        limit));
    }

    private static Finding Finding(IReadOnlySet<string> group, IReadOnlyList<FunctionUnit> functions, int limit)
    {
        var first = functions[0];
        var names = string.Join(", ", group.Order(StringComparer.Ordinal));
        var message = $"DS02: parameters ({names}) travel together through {functions.Count} functions, limit {limit}: "
            + $"{Functions.Listed(functions)}. Make the group a type and pass that.";
        return new Finding(
            "DS02",
            first.File.Path,
            first.Line,
            message,
            $"DS02|{first.File.Path}|{first.Id}|{names}",
            false);
    }

    private static bool IsNamed(FunctionUnit unit) =>
        unit.Symbol.MethodKind != MethodKind.AnonymousFunction && unit.Symbol.AssociatedSymbol is null;

    private static Signature SignatureOf(FunctionUnit unit)
    {
        var parameters = unit.Symbol.Parameters.Where(parameter => !Symbols.IsReceiver(parameter))
            .Select(parameter => $"{parameter.Type.ToDisplayString()} {parameter.Name}").ToHashSet();
        return new Signature(unit, SymbolIds.Of(Root(unit.Symbol)), parameters);
    }

    /// <summary>The method a method overrides or implements, followed to the first declaration; else the method
    /// itself.</summary>
    private static IMethodSymbol Root(IMethodSymbol method)
    {
        var implemented = method.ContainingType.AllInterfaces.SelectMany(
            contract => contract.GetMembers().OfType<IMethodSymbol>())
            .FirstOrDefault(
                member =>
                    SymbolEqualityComparer.Default.Equals(
                        method.ContainingType.FindImplementationForInterfaceMember(member),
                        method));
        var above = method.OverriddenMethod ?? implemented;
        return above is null ? method.OriginalDefinition : Root(above);
    }

    /// <summary>Every set of three or more parameters that two functions have in common.</summary>
    private static IEnumerable<IReadOnlySet<string>> Shared(IReadOnlyList<Signature> signatures) =>
        signatures.SelectMany((left, index) => signatures.Skip(index + 1)
                .Select(right => left.Parameters.Intersect(right.Parameters).Order(StringComparer.Ordinal).ToList()))
            .Where(common => common.Count >= GroupSize)
            .DistinctBy(common => string.Join("|", common))
            .Select(common => common.ToHashSet());

    private static IReadOnlyList<Signature> Carriers(IReadOnlySet<string> group, IReadOnlyList<Signature> signatures) =>
        signatures.Where(signature => group.IsSubsetOf(signature.Parameters)).ToList();
}
