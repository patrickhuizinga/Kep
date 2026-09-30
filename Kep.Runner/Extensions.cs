namespace Kep.Runner;

public static class Extensions
{
    public static double? ToDouble(this bool? b)
        => b.HasValue
            ? b.Value ? 1 : 0
            : null;
}
