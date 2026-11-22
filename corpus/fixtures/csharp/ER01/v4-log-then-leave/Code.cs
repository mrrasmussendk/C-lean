using System;

namespace Fixture;

public static class Code
{
    public static void Run(string text)
    {
        try
        {
            Console.WriteLine(int.Parse(text));
        }
        catch (FormatException error)
        {
            Console.WriteLine(error.Message);
            return;
        }
    }
}
