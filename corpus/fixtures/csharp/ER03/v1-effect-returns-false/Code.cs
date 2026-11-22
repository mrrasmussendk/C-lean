using System.IO;

namespace Fixture;

public static class Code
{
    public static bool Save(string path, string text)
    {
        if (text.Length == 0)
        {
            return false;
        }
        File.WriteAllText(path, text);
        return true;
    }
}
