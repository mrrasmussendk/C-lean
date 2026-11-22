namespace Fixture;

public static class Code
{
    public static bool Load(
        string text,
        Result result)
    {
        result.Length = text.Length;
        return text.Length > 0;
    }
}

public class Result
{
    public int Length;
}
