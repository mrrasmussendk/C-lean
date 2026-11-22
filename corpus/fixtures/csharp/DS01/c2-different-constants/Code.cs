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

    public static int IsStrike(int pins)
    {
        if (pins == PinCount)
        {
            return 1;
        }
        return 0;
    }

    public static int IsFull(int rolls)
    {
        if (rolls >= RollCount)
        {
            return 1;
        }
        return 0;
    }
}
