namespace Fixture;

public static class Code
{
    public static int Clamp(int count)
    {
        bool tooMany = count > 10;
        if (tooMany)
        {
            return 10;
        }
        return count;
    }
}
