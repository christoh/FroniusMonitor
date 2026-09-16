namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// <c>Type.GetSize()</c> answers how many bytes a value of the type occupies, which is what the Modbus and SunSpec
/// mapping needs to lay registers out. An enum has to answer for its underlying type, not for the enum.
/// </summary>
public sealed class GetManagedSizeTests
{
    private enum ByteEnum : byte { }

    private enum UShortEnum : ushort { }

    private enum UIntEnum : uint { }

    private static readonly IReadOnlyList<(Type Type, int Size)> Expected =
    [
        (typeof(byte), 1),
        (typeof(ByteEnum), 1),
        (typeof(sbyte), 1),
        (typeof(short), 2),
        (typeof(ushort), 2),
        (typeof(UShortEnum), 2),
        (typeof(int), 4),
        (typeof(uint), 4),
        (typeof(UIntEnum), 4),
        (typeof(long), 8),
        (typeof(ulong), 8),
        (typeof(float), 4),
        (typeof(double), 8),
        (typeof(decimal), 16),
    ];

    [Fact]
    public void Every_primitive_and_every_enum_answers_the_size_of_its_underlying_type()
    {
        foreach (var (type, size) in Expected)
        {
            Assert.Equal(size, type.GetSize());
        }
    }

    [Fact]
    public async Task The_answers_stay_right_when_many_threads_ask_at_once()
    {
        // GetSize caches what it worked out, and the cache is the thing under test here. The NUnit original
        // started these tasks inside a Parallel.For and never awaited them, so a wrong answer died with its task
        // and the test passed regardless; awaiting them is what gives it the power to fail.
        var tasks = Enumerable.Range(0, 200)
            .SelectMany(_ => Expected.Select(e => Task.Run(() => Assert.Equal(e.Size, e.Type.GetSize()))));

        await Task.WhenAll(tasks);
    }
}
