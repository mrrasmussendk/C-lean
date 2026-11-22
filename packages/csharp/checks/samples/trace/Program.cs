using Sample.Audit;
using Sample.Report;
using Sample.Store;

namespace Sample;

/// <summary>Sample for the instrument check: asserts its own results and exits non-zero on a failure.</summary>
internal static class Program
{
    private static int Main() => Run().GetAwaiter().GetResult();

    private static async Task<int> Run()
    {
        var repository = new Repository();
        var seen = 0;
        foreach (var item in repository.Items())
        {
            Printer.Print(item);
            seen += item;
        }
        Check(seen == 6, "the iterator yields 1, 2, 3");
        var loaded = await repository.LoadAsync(4);
        Check(loaded == 10, "the async method returns id + total");
        Printer.Print(loaded);
        var doubled = repository.Apply(value => { return Twice(value); });
        Check(doubled == 2, "the lambda is applied to the first item");
        try
        {
            Repository.Fail();
            Check(false, "Fail throws");
        }
        catch (InvalidOperationException error)
        {
            Check(error.Message == "boom", "the exception keeps its message");
        }
        Printer.Print(Journal.Count);
        Console.WriteLine($"ok: seen {seen}, loaded {loaded}, doubled {doubled}, printed {Printer.Printed}");
        return 0;

        int Twice(int value) => value * 2;
    }

    private static void Check(bool holds, string what)
    {
        if (!holds)
        {
            Console.Error.WriteLine("FAILED: " + what);
            Environment.Exit(1);
        }
    }
}
