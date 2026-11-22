namespace Fixture;

public static class Code
{
    public static int Run(int x)
    {
        int total = 0;
        while (total < 0)
        {
            total += 1;
        }
        if (x > 1)
        {
            total += 1;
        }
        while (total < 2)
        {
            total += 1;
        }
        if (x > 3)
        {
            total += 1;
        }
        while (total < 4)
        {
            total += 1;
        }
        if (x > 5)
        {
            total += 1;
        }
        while (total < 6)
        {
            total += 1;
        }
        if (x > 7)
        {
            total += 1;
        }
        while (total < 8)
        {
            total += 1;
        }
        if (x > 9)
        {
            total += 1;
        }
        return total;
    }

    private static readonly object Gate = new object();
}
