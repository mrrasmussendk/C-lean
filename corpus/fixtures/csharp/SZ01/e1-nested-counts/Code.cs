namespace Fixture;

public static class Code
{
    public static int Outer()
    {
        int total = 0;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        total += 1;
        int Inner()
        {
            int inner = 0;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            inner += 1;
            return inner;
        }
        return total + Inner();
    }
}
