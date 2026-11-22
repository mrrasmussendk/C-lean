using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Gate.CSharp.Output;

internal static class Json
{
    private static readonly JsonWriterOptions Indented = new() { Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Render(Action<Utf8JsonWriter> write)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, Indented))
        {
            write(writer);
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
