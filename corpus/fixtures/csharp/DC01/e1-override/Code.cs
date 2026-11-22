namespace Fixture;

public abstract class Job
{
    public int Run()
    {
        return Step();
    }

    internal abstract int Step();
}

public class PrintJob : Job
{
    internal override int Step()
    {
        return 1;
    }
}
