namespace Fixture;

public static class Code
{
    public static int Clamp(int count)
    {
        if (count > 10)
        {
            return 10;
        }
        return count;
    }
}
