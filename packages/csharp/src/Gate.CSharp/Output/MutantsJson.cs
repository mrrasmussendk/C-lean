using System.Collections.Generic;
using System.Text.Json;
using Gate.CSharp.Model;

namespace Gate.CSharp.Output;

/// <summary>mutants.json: every mutant with its byte range and replacement.</summary>
internal static class MutantsJson
{
    public static string Render(IReadOnlyList<Mutant> mutants) => Json.Render(writer => Document(writer, mutants));

    private static void Document(Utf8JsonWriter writer, IReadOnlyList<Mutant> mutants)
    {
        writer.WriteStartObject();
        writer.WriteString("spec", Sarif.SpecVersion);
        writer.WriteString("language", "csharp");
        writer.WriteStartArray("mutants");
        foreach (var mutant in mutants)
        {
            Mutant(writer, mutant);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Mutant(Utf8JsonWriter writer, Mutant mutant)
    {
        writer.WriteStartObject();
        writer.WriteString("id", mutant.Id);
        writer.WriteString("file", mutant.File);
        writer.WriteNumber("line", mutant.Line);
        writer.WriteNumber("column", mutant.Column);
        writer.WriteString("mutator", mutant.Mutator);
        writer.WriteNumber("start", mutant.Start);
        writer.WriteNumber("end", mutant.End);
        writer.WriteString("original", mutant.Original);
        writer.WriteString("replacement", mutant.Replacement);
        writer.WriteString("context", mutant.Context);
        writer.WriteEndObject();
    }
}
