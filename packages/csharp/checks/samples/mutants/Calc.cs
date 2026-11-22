// Sample for the mutants check. This file starts with a byte order mark; "ærø – 日本" shifts byte offsets.
using System;

namespace Sample;

public enum Level
{
    Low = 1 + 1,
}

public static class Calc
{
    private const int Limit = 10 - 2;
    private static readonly bool Strict = true;

    public static int Price(int amount, int count)
    {
        string label = "prisen på ærø" + amount;
        if (amount < 0 || count == 0)
        {
            throw new ArgumentException(label);
        }
        Console.WriteLine("total " + (amount * count));
        bool cheap = !(amount >= Limit) && Strict;
        return cheap ? amount + count : amount / count - 1;
    }

    [Obsolete("old" + "er", false)]
    public static bool Same(int a, int b) => a != b ? false : a <= b;
}
