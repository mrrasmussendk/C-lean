using System.Collections.Generic;
using System.Text.Json;
using Gate.CSharp.Model;

namespace Gate.CSharp.Output;

/// <summary>units.json, per spec/schemas/units.schema.json.</summary>
internal static class UnitsJson
{
    public static string Render(IReadOnlyList<UnitRow> units) => Json.Render(writer => Document(writer, units));

    private static void Document(Utf8JsonWriter writer, IReadOnlyList<UnitRow> units)
    {
        writer.WriteStartObject();
        writer.WriteString("spec", Sarif.SpecVersion);
        writer.WriteString("language", "csharp");
        writer.WriteStartArray("units");
        foreach (var unit in units)
        {
            Unit(writer, unit);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Unit(Utf8JsonWriter writer, UnitRow unit)
    {
        writer.WriteStartObject();
        writer.WriteString("id", unit.Id);
        writer.WriteString("file", unit.File);
        writer.WriteNumber("startLine", unit.StartLine);
        writer.WriteNumber("endLine", unit.EndLine);
        writer.WriteNumber("sloc", unit.Sloc);
        writer.WriteString("hash", unit.Hash);
        writer.WriteBoolean("test", unit.Test);
        writer.WriteNumber("callers", unit.Callers);
        writer.WriteStartArray("sequences");
        foreach (var sequence in unit.Sequences)
        {
            Sequence(writer, sequence);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Sequence(Utf8JsonWriter writer, SequenceRow sequence)
    {
        writer.WriteStartObject();
        writer.WriteString("hash", sequence.Hash);
        writer.WriteNumber("startLine", sequence.StartLine);
        writer.WriteNumber("endLine", sequence.EndLine);
        writer.WriteEndObject();
    }
}
