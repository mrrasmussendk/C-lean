using System.IO;

namespace Fixture;

public static class Code
{
    public static bool Save(string path, string text)
    {
        File.WriteAllText(path, text);
        return text.Length > 0;
    }
}
