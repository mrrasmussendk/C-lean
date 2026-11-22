namespace Fixture;

public static class Code
{
    public static int IsRound(object shape)
    {
        if (shape is Circle)
        {
            return 1;
        }
        return 0;
    }

    public static int Width(object shape)
    {
        var circle = shape as Circle;
        if (circle != null)
        {
            return circle.Radius;
        }
        return 0;
    }

    public static int Height(object shape)
    {
        return shape switch
        {
            Circle circle => circle.Radius,
            _ => 0,
        };
    }
}

public class Circle
{
    public int Radius;
}
