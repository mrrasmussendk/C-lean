namespace Fixture;

public abstract class Ball
{
    public virtual double Speed(double aim, double power, double drift)
    {
        return aim + power + drift;
    }
}

public class HeavyBall : Ball
{
    public override double Speed(double aim, double power, double drift)
    {
        return aim + power - drift;
    }
}

public class LightBall : Ball
{
    public override double Speed(double aim, double power, double drift)
    {
        return aim - power + drift;
    }
}
