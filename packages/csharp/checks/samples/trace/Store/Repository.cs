using Sample.Audit;

namespace Sample.Store;

public sealed class Repository
{
    private readonly List<int> _items;

    public Repository() => _items = [1, 2, 3];

    public int Total => _items.Sum();

    public int this[int index] => _items[index];

    public static int Fail() => throw new InvalidOperationException("boom");

    public IEnumerable<int> Items()
    {
        foreach (var item in _items)
        {
            if (item > 0)
                yield return item;
        }
    }

    public async Task<int> LoadAsync(int id)
    {
        await Task.Yield();
        var recorded = await Journal.RecordAsync(id);
        return recorded + Total;
    }

    public int Apply(Func<int, int> change) => change(this[0]);
}
