namespace Fixture;

public static class Code
{
    public static int Take(
        bool flag,
        int count)
    {
        if (flag && count > 0)
        {
            return count;
        }
        return 0;
    }
}
