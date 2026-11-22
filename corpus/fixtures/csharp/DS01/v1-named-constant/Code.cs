namespace Fixture;

public static class Code
{
    private const int FrameCount = 10;
    private const int PinCount = 10;
    private const int RollCount = 21;

    public static int IsLast(int frame)
    {
        if (frame == FrameCount)
        {
            return 1;
        }
        return 0;
    }

    public static int IsOpen(int index)
    {
        if (index < FrameCount)
        {
            return 1;
        }
        return 0;
    }

    public static int IsBonus(int number)
    {
        if (number >= FrameCount)
        {
            return 1;
        }
        return 0;
    }

    public static int Total()
    {
        return PinCount + RollCount;
    }
}
