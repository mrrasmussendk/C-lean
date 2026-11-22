using System.IO;

namespace Fixture;

public static class Code
{
    public static void Save(string path, string text)
    {
        if (text.Length == 0)
        {
            throw new System.ArgumentException("empty");
        }
        File.WriteAllText(path, text);
    }
}
