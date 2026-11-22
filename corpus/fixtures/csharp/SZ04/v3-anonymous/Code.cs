using System;

namespace Fixture;

public static class Code
{
    public static readonly Func<int, int, int, int, int, int> Sum =
        (a, b, c, d, e) => a + b + c + d + e;
}
