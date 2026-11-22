namespace Fixture;

public static class Code
{
    public static double Speed(double aim, double power, double drift)
    {
        return aim + power + drift;
    }

    public static double Curve(double aim, double power, double drift)
    {
        return aim * drift - power;
    }

    public static double Reach(double aim, double power, double drift)
    {
        return power - aim - drift;
    }
}
