namespace Fixture;

public static class Code
{
    public static int left(int count, int step)
    {
        int total = 0;
        int index = 0;
        while (index < count)
        {
            total = total + step;
            index = index + 1;
        }
        string label = "sum";
        object mark = 2;
        Report(label, total);
        int scaled = total * 2;
        return scaled;
    }

    public static int right(int count, int step)
    {
        int total = 5;
        int index = 5;
        while (index < count)
        {
            total = total + step;
            index = index + 3;
        }
        string label = "all";
        object mark = 9;
        Report(label, total);
        int scaled = total * 7;
        return scaled;
    }

    private static void Report(string name, int value)
    {
    }

    private static void Report(string name, int value, int extra)
    {
    }

    private static void Publish(string title, int amount)
    {
    }
}
