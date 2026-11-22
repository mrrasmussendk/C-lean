namespace Fixture;

public static class Code
{
    public static int Run(int x)
    {
        int total = 0;
        foreach (var item in new[] { x })
        {
            if (item > 0)
            {
                try
                {
                    if (item > 1)
                    {
                        total += 1;
                    }
                }
                finally
                {
                    total += 1;
                }
            }
        }
        return total;
    }

    private static readonly object Gate = new object();
}
