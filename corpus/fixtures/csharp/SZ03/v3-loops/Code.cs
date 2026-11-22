namespace Fixture;

public static class Code
{
    public static int Run(int x)
    {
        int total = 0;
        for (int i0 = 0; i0 < x; i0++)
        {
            for (int i1 = 0; i1 < x; i1++)
            {
                for (int i2 = 0; i2 < x; i2++)
                {
                    for (int i3 = 0; i3 < x; i3++)
                    {
                        total += 1;
                    }
                }
            }
        }
        return total;
    }

    private static readonly object Gate = new object();
}
