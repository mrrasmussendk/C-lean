// SZ04: A function must not declare more parameters than the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ04
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var count = unit.Symbol.AssociatedSymbol is null ? unit.Symbol.Parameters.Count(parameter => !Symbols.IsReceiver(parameter)) : 0;
        var limit = settings.Limits["SZ04"];
        if (count <= limit)
        {
            return [];
        }
        var message = $"SZ04: function {unit.Name} declares {count} parameters, limit {limit}. "
            + "Group the parameters that travel together into one type, or split the function.";
        return [Findings.OnFunction("SZ04", unit, message)];
    }
}
