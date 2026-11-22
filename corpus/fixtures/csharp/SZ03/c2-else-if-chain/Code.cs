namespace Fixture;

public static class Code
{
    public static int Run(int x)
    {
        int total = 0;
        if (x == 0)
        {
            total += 1;
        }
        else if (x == 1)
        {
            total += 1;
        }
        else if (x == 2)
        {
            total += 1;
        }
        else if (x == 3)
        {
            total += 1;
        }
        else if (x == 4)
        {
            total += 1;
        }
        else if (x == 5)
        {
            total += 1;
        }
        else if (x == 6)
        {
            total += 1;
        }
        return total;
    }

    private static readonly object Gate = new object();
}
