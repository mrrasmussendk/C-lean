// FX01: Effect APIs (clock, file system, network, console, randomness, environment, mutable global state) may be called only inside components marked as effect components in the architecture model
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class FX01
{
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings)
    {
        if (file.IsEffect)
        {
            return [];
        }
        var nodes = file.Tree.GetRoot().DescendantNodes().ToList();
        var calls = EffectApis.Uses(nodes, file.Model).Select(use => (Line: Lines.Of(use.Site), Api: $"{use.Api} ({use.Group})"));
        var writes = StateWrites.In(nodes, file.Model).Where(StateWrites.IsMutableStatic)
            .Select(write => (Line: Lines.Of(write.Site), Api: $"assignment to static {write.Target.Name} (mutable global state)"));
        return calls.Concat(writes).Distinct().Select(use => Findings.OnLine("FX01", file, use.Line, Message(use.Api, file)));
    }

    private static string Message(string api, SourceFile file) =>
        $"FX01: {api} is an effect used in component '{file.Component}', which is not an effect component. "
        + "Move the call into an effect component and pass its result in.";
}
