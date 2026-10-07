using HKW.MapperTest;

internal class Program
{
    internal static void Main(string[] args)
    {
        var i1 = 0;
        var i2 = 0;

        (i1, i2) = GetInt();
    }

    public static (int, int) GetInt()
    {
        return (1, 2);
    }
}
