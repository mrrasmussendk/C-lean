// FX02: Command-query separation: a function that returns a value must not assign to instance, static or module state
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class FX02
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var assigned = StateWrites.In(Functions.OwnNodes(unit.Body), unit.File.Model).Select(write => write.Target.Name).Distinct().ToList();
        if (!Returns.HasValue(unit.Symbol) || assigned.Count == 0)
        {
            return [];
        }
        var message = $"FX02: function {unit.Name} returns a value and assigns state ({string.Join(", ", assigned)}). "
            + "Split it into a command that changes state and a query that returns the value.";
        return [Findings.OnFunction("FX02", unit, message)];
    }
}
