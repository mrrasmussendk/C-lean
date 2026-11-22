namespace Sample.Report;

public static class Printer
{
    public static int Printed { get; private set; }

    public static void Print(int value) => Printed += value;
}
