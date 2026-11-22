using System.Linq.Expressions;

namespace Sample;

/// <summary>Sample for the switch check: prints results that change when a mutant is active.</summary>
internal static class Program
{
    private static int Main()
    {
        Console.WriteLine($"price {Pricing.Price(4, 3)} {Pricing.Price(20, 2)}");
        Console.WriteLine($"label {Pricing.Label(5)} {Pricing.Label(-5)} sum={Pricing.Price(1, 1) + 2:D3}");
        Console.WriteLine($"parse {Pricing.Parse("12")} {Pricing.Parse("x")} {Pricing.Parse(null)}");
        Console.WriteLine($"kind {Pricing.Kind(2)} {Pricing.Kind(7)} flag {Pricing.Flag()} {Pricing.Flag(false)}");
        Console.WriteLine($"count {Pricing.Count(3)} tree {Pricing.Compiled()(5)} length {Pricing.Length("abc")} {Pricing.Length(null)}");
        return Pricing.Price(2, 2) == 4 ? 0 : 3;
    }
}

public static class Pricing
{
    private const int Limit = 10 - 2;

    public static int Price(int amount, int count)
    {
        bool cheap = !(amount >= Limit) && count > 1;
        return cheap ? amount + count : amount / count - 1;
    }

    public static string Label(int value) => value < 0 || value == 0 ? "none" : "some" + value;

    public static int Parse(string? text)
    {
        if (text != null && int.TryParse(text, out var number))
        {
            return number * 2;
        }
        return -1;
    }

    public static string Kind(int value)
    {
        switch (value)
        {
            case 1 + 1:
                return "two";
            default:
                return value is > 5 ? "big" : "small";
        }
    }

    public static bool Flag(bool on = true) => on == true;

    public static int Count(int upTo)
    {
        var total = 0;
        while (true)
        {
            if (total >= upTo || total < -upTo)
            {
                return total;
            }
            total = total + 1;
        }
    }

    public static Func<int, bool> Compiled()
    {
        Expression<Func<int, bool>> big = value => value > 3;
        return big.Compile();
    }

    /// <summary>Never called by Main: its mutants must not be reported as reached.</summary>
    public static bool Unused(int left, int right) => left + right > 0 && left != right;

    public static int Length(string? text)
    {
        if (text == null)
        {
            return 0;
        }
        return text.Length * 1;
    }
}
