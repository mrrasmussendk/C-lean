// SZ05: Type SLOC must not exceed the limit
using System.Collections.Generic;
using System.Linq;
using Gate.CSharp.Analysis;
using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gate.CSharp.Rules;

internal static class SZ05
{
    public static IEnumerable<Finding> Check(SourceFile file, Settings settings)
    {
        var limit = settings.Limits["SZ05"];
        return file.Tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .Select(type => (Symbol: file.Model.GetDeclaredSymbol(type), Sloc: Sloc.Of(type)))
            .Where(type => type.Sloc > limit && type.Symbol is not null)
            .Select(
                type => Findings.OnSymbol("SZ05", file, type.Symbol!, Message(type.Symbol!.Name, type.Sloc, limit)));
    }

    private static string Message(string name, int sloc, int limit) =>
        $"SZ05: type {name} has {sloc} SLOC, limit {limit}. Move a cohesive group of members into a type of its own.";
}
