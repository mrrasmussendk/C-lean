namespace Fixture;

public static class Code
{
    public static bool ShouldStop(int retries)
    {
        if (retries > 3)
        {
            return true;
        }
        return false;
    }
}
