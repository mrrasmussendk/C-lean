namespace Fixture;

public static class Code
{
    public static double Speed(Throw ball)
    {
        return ball.Aim + ball.Power + ball.Drift;
    }

    public static double Curve(Throw ball)
    {
        return ball.Aim * ball.Drift - ball.Power;
    }

    public static double Reach(Throw ball)
    {
        return ball.Power - ball.Aim - ball.Drift;
    }
}

public record Throw(double Aim, double Power, double Drift);
