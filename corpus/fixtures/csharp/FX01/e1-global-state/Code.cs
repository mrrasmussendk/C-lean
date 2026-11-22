using System;

namespace Fixture;

public static class Code
{
    private static int _counter;

    public static void Bump()
    {
        _counter = _counter + 1;
    }

    public static int Read()
    {
        return _counter;
    }
}
