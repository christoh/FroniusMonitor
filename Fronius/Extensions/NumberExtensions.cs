namespace De.Hochstaetter.Fronius.Extensions;

public static class NumberExtensions
{
    public static T Max<T>(params IEnumerable<T> numbers) where T : struct => numbers.Max();
    public static T Min<T>(params IEnumerable<T> numbers) where T : struct => numbers.Min();
}