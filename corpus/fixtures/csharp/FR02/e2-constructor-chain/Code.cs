namespace Fixture;

public class Shape
{
    private readonly int _area;

    public Shape(int width, int height)
    {
        _area = width * height;
    }

    public int Area()
    {
        return _area;
    }
}

public class Square : Shape
{
    public Square(int width, int height)
        : base(width, height)
    {
    }
}
