namespace Fixture;

public static class Code
{
    private const int SecondsPerHour = 3600;

    public static int Seconds(int hours)
    {
        return hours * SecondsPerHour;
    }
}
