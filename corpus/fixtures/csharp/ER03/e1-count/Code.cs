using System.IO;

namespace Fixture;

public static class Code
{
    public static int SaveAll(string path, string[] texts)
    {
        int saved = 0;
        foreach (var text in texts)
        {
            File.AppendAllText(path, text);
            saved = saved + 1;
        }
        return saved;
    }
}
