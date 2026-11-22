namespace Fixture;

public static class Code
{
    public static int IsLast(int frame)
    {
        if (frame == 10)
        {
            return 1;
        }
        return 0;
    }

    public static int IsOpen(int index)
    {
        if (index < 10)
        {
            return 1;
        }
        return 0;
    }

    public static int IsBonus(int number)
    {
        if (number >= 10)
        {
            return 1;
        }
        return 0;
    }
}
