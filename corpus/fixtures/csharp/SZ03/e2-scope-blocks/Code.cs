using System.IO;

namespace Fixture;

public static class Code
{
    public static int Run(int x)
    {
        int total = 0;
        using (var stream = new MemoryStream())
        {
            lock (Gate)
            {
                if (x > 0)
                {
                    for (int i = 0; i < x; i++)
                    {
                        if (i > 0)
                        {
                            total += 1;
                        }
                    }
                }
            }
        }
        return total;
    }

    private static readonly object Gate = new object();
}
