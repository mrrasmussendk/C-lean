namespace Fixture;

public static class Code
{
    private const int FrameCount = 10;
    private const int PinCount = 10;
    private const int RollCount = 21;

    public static int First(int start)
    {
        int total = start;
        for (int index = 0; index < FrameCount; index++)
        {
            total = total + FrameCount;
        }
        return total;
    }

    public static int Second(int start)
    {
        int total = start;
        for (int index = 0; index < FrameCount; index++)
        {
            total = total + FrameCount;
        }
        return total;
    }

    public static int Third(int start)
    {
        int total = start;
        for (int index = 0; index < FrameCount; index++)
        {
            total = total + FrameCount;
        }
        return total;
    }

    public static int Total()
    {
        return PinCount + RollCount;
    }
}
