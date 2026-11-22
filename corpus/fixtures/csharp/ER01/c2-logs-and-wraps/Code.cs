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
            Logger.LogError(error.Message);
            throw new InvalidOperationException("not a number", error);
        }
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
