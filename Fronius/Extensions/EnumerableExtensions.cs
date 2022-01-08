namespace De.Hochstaetter.Fronius.Extensions;

public static class EnumerableExtensions
{
    public static void Apply<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }
}