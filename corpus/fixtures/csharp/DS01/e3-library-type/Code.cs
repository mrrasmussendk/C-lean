using System.Text.Json;

namespace Fixture;

public static class Code
{
    public static string Text(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? "";
        }
        return "";
    }

    public static int Size(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Array ? element.GetArrayLength() : 0;
    }

    public static bool IsObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return true;
            default:
                return false;
        }
    }
}
