using HKW.MapperTest;

internal class Program
{
    internal static void Main(string[] args)
    {
        Value1 = (CloneableValue)Value2?.Clone()!;
    }

    public static CloneableValue Value1 { get; set; }
    public static CloneableValue? Value2 { get; set; }
}
