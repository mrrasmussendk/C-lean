using System;

namespace Fixture;

public static class Code
{
    public static readonly Func<bool, int> Pick =
        flag => flag ? 1 : 2;
}
