using System;

namespace Fixture;

public static class Clock
{
    public static DateTime Now()
    {
        return DateTime.UtcNow;
    }
}
