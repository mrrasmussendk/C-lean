using System;

namespace Fixture;

public class Code
{
    private int _failures;

    public int Run(string text)
    {
        try
        {
            return int.Parse(text);
        }
        catch (FormatException error)
        {
            Catalog.Restore(error.Message);
            Dialog.ShowErrorDialog(error.Message);
        }
        return 0;
    }

    public int Failures()
    {
        return _failures;
    }
}

public static class Logger
{
    public static void LogError(string text)
    {
    }
}

public static class Catalog
{
    public static void Restore(string text)
    {
    }
}

public static class Dialog
{
    public static void ShowErrorDialog(string text)
    {
    }
}
