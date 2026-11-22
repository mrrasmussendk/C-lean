namespace Fixture;

public static class Code
{
    public static double Speed(double aim, double power, double wind)
    {
        return aim + power + wind;
    }

    public static double Curve(double aim, double power, double spin)
    {
        return aim * spin - power;
    }

    public static double Reach(double aim, double power, double slope)
    {
        return power - aim - slope;
    }
}
