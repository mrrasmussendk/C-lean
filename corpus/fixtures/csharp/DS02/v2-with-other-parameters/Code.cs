namespace Fixture;

public static class Code
{
    public static double Speed(int lane, double aim, double power, double drift)
    {
        return lane + aim + power + drift;
    }

    public static double Curve(double drift, double aim, string name, double power)
    {
        return aim * drift - power + name.Length;
    }

    public static double Reach(double aim, double power, double drift, bool wet)
    {
        return wet ? power : power - aim - drift;
    }
}
