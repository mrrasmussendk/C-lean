using System;

namespace Fixture;

public static class Code
{
    public static string Label(int a, int b)
    {
        return (a + b).ToString().Trim().ToUpperInvariant();
    }
}
