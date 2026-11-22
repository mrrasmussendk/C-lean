namespace Fixture;

public static class Code
{
    public static int Size(string text)
    {
        return text.Trim().ToUpperInvariant().Substring(1).Split(',').Length;
    }
}
