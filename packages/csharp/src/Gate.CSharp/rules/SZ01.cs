// SZ01: Function body SLOC must not exceed the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ01
{
    public static IEnumerable<Finding> Check(FunctionUnit unit, Settings settings)
    {
        var sloc = Sloc.Of(unit.Body);
        var limit = settings.Limits["SZ01"];
        if (sloc <= limit)
        {
            return [];
        }
        var message = $"SZ01: function {unit.Name} has {sloc} SLOC, limit {limit}. "
            + "Split it at a point where the extracted part has a name that is not a restatement of its body.";
        return [Findings.OnFunction("SZ01", unit, message)];
    }
}
