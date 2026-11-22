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
        finally
        {
            _failures = 0;
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
