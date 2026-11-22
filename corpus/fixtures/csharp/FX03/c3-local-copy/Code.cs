using System.Collections.Generic;
using System.Linq;

namespace Fixture;

public static class Code
{
    public static List<int> WithOne(List<int> items)
    {
        var copy = new List<int>(items);
        copy.Add(1);
        return copy;
    }
}
