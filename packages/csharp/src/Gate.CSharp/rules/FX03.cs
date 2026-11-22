// FX03: A function must not mutate an object it received as an argument
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class FX03
{
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings) =>
        Mutations.In(file.Tree.GetRoot(), file.Model)
            .Select(mutation => (Line: Lines.Of(mutation.Site), mutation.Target.Name))
            .Distinct()
            .Select(mutation => Findings.OnLine("FX03", file, mutation.Line, Message(mutation.Name)));

    private static string Message(string parameter) =>
        $"FX03: the object received as parameter {parameter} is mutated. "
        + "Build and return a new value, or make the change a method of the object's own type.";
}
