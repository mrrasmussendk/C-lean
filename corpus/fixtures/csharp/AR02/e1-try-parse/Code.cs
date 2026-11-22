namespace Fixture;

public static class Code
{
    public static bool TryParse(string text, out int value)
    {
        return int.TryParse(text, out value);
    }
}
