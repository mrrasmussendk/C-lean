using System.IO;

namespace Fixture;

public static class Code
{
    public static Outcome Save(string path, string text)
    {
        if (text.Length == 0)
        {
            return Outcome.Rejected;
        }
        File.WriteAllText(path, text);
        return Outcome.Saved;
    }
}

public enum Outcome
{
    Saved,
    Rejected,
}
