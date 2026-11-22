using System;

namespace Fixture;

public static class Code
{
    public static int Run(int x)
    {
        int total = 0;
        if (x > 0)
        {
            foreach (var item in new[] { x })
            {
                Func<int, int> inner = value =>
                {
                    if (value > 0)
                    {
                        while (value > 10)
                        {
                            value -= 1;
                        }
                    }
                    return value;
                };
                total += inner(item);
            }
        }
        return total;
    }

    private static readonly object Gate = new object();
}
