namespace Sample.Audit;

public static class Journal
{
    private static int _count;

    public static int Count
    {
        get => _count;
    }

    public static async Task<int> RecordAsync(int id)
    {
        await Task.Delay(1);
        _count++;
        return id;
    }
}
