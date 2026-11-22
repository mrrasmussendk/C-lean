namespace Fixture;

public class Code
{
    private readonly Sender _inner = new Sender();

    public int Send(string to, string subject, string text)
    {
        return _inner.Send(to, subject, text);
    }
}

public class Sender
{
    public int Send(string to, string subject, string text)
    {
        return to.Length + subject.Length + text.Length;
    }
}
