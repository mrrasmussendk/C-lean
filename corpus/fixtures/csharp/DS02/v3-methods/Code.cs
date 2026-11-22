namespace Fixture;

public class Painter
{
    public int Fill(int left, int top, int width)
    {
        return left + top + width;
    }

    public int Stroke(int left, int top, int width)
    {
        return left * top * width;
    }
}

public class Eraser
{
    public int Clear(int left, int top, int width)
    {
        return left - top - width;
    }
}
