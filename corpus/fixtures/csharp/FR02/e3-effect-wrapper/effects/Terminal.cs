using System;
using System.IO;

namespace Fixture;

public static class Terminal
{
    public static void Print(string text)
    {
        Console.WriteLine(text);
    }

    public static string Load(string path)
    {
        return File.ReadAllText(path);
    }
}
