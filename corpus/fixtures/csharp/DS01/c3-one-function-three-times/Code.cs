namespace Fixture;

public static class Code
{
    private const int FrameCount = 10;
    private const int PinCount = 10;
    private const int RollCount = 21;

    public static int Rank(int frame)
    {
        if (frame == FrameCount)
        {
            return 2;
        }
        if (frame > FrameCount)
        {
            return 3;
        }
        if (frame + 1 == FrameCount)
        {
            return 1;
        }
        return PinCount + RollCount;
    }
}
