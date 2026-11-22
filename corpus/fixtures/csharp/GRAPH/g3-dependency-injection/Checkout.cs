namespace Fixture;

public class Checkout
{
    private readonly INotifier _notifier;

    public Checkout(INotifier notifier)
    {
        _notifier = notifier;
    }

    public void Finish()
    {
        _notifier.Send("done");
    }
}

public static class Root
{
    public static Checkout Build()
    {
        return new Checkout(new MailNotifier());
    }
}
