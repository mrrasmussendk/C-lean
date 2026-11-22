using System;

namespace Fixture;

public static class Code
{
    public static void Show()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        Console.WriteLine(home);
    }
}
