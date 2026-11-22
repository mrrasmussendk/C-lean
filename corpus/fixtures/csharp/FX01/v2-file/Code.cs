using System.IO;

namespace Fixture;

public static class Code
{
    public static string Load(string path)
    {
        return File.ReadAllText(path);
    }
}
