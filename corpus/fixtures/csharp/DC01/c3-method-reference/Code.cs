using System.Collections.Generic;
using System.Linq;

namespace Fixture;

public static class Code
{
    public static List<int> Run(List<int> items)
    {
        return items.Select(Twice).ToList();
    }

    private static int Twice(int value)
    {
        return value + value;
    }
}
