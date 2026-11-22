using Gate.CSharp.Model;
using Microsoft.CodeAnalysis;

namespace Gate.CSharp.Analysis;

/// <summary>Builds findings with the gate/v1 fingerprint: the symbol ID, or the hash of the trimmed source
/// line.</summary>
internal static class Findings
{
    public static Finding OnFunction(string rule, FunctionUnit unit, string message) =>
        new(rule, unit.File.Path, unit.Line, message, $"{rule}|{unit.File.Path}|{unit.Id}", false);

    public static Finding OnSymbol(string rule, SourceFile file, ISymbol symbol, string message) =>
        new(rule, file.Path, Lines.Of(symbol, file.Tree), message, $"{rule}|{file.Path}|{SymbolIds.Of(symbol)}", false);

    public static Finding OnLine(string rule, SourceFile file, int line, string message) =>
        new(rule, file.Path, line, message, $"{rule}|{file.Path}|{Hashing.Sha256(LineText(file, line))}", false);

    private static string LineText(SourceFile file, int line) =>
        file.Tree.GetText().Lines[line - 1].ToString().Trim();
}
