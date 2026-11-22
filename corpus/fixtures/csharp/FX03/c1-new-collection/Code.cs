using System.Collections.Generic;
using System.Linq;

namespace Fixture;

public static class Code
{
    public static List<int> Doubled(List<int> items)
    {
        return items.Select(item => item * 2).ToList();
    }
}
