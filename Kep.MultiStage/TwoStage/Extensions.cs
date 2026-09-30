namespace Kep.MultiStage.TwoStage;

public static class Extensions
{
    public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
        => enumerable.ToList();
}
