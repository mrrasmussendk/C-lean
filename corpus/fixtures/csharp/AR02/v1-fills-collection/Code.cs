using System.Collections.Generic;

namespace Fixture;

public static class Code
{
    public static void Collect(
        int count,
        out List<int> results)
    {
        results = new List<int>();
        results.Add(count);
    }
}
