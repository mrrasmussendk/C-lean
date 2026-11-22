namespace Fixture;

public static class Code
{
    public static void Go(D d)
    {
        d.Run();
    }
}

public class A
{
    public B B = new B();

    public B GetB()
    {
        return B;
    }
}

public class B
{
    public C C = new C();

    public C GetC()
    {
        return C;
    }

    public void Run()
    {
    }
}

public class C
{
    public D D = new D();

    public void Run()
    {
    }
}

public class D
{
    public void Run()
    {
    }
}
