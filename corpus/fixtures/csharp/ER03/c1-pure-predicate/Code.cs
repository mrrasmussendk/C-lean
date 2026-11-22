namespace Fixture;

public static class Code
{
    public static bool IsEmpty(string text)
    {
        if (text.Length > 0)
        {
            return false;
        }
        return true;
    }
}
