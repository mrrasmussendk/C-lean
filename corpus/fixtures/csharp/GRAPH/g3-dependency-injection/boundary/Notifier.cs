namespace Fixture;

public interface INotifier
{
    void Send(string text);
}

public class MailNotifier : INotifier
{
    public void Send(string text)
    {
    }
}
