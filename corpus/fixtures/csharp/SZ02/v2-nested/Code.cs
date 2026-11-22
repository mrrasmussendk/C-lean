namespace Fixture;

public static class Code
{
    public static int Run(bool a, bool b, bool c, bool d, int x)
    {
        int total = 0;
        for (int i = 0; i < x; i++)
        {
            if (a)
            {
                while (b)
                {
                    if (c)
                    {
                        total += 1;
                    }
                }
            }
        }
        for (int j = 0; j < x; j++)
        {
            if (a)
            {
                if (d)
                {
                    total += 1;
                }
            }
        }
        return total;
    }
}
