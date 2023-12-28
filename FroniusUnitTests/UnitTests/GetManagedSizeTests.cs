using System.Threading;
using De.Hochstaetter.Fronius.Extensions;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace FroniusUnitTests.UnitTests;

[TestFixture]
public class GetManagedSizeTests
{
    private enum ByteEnum : byte { }
    private enum UShortEnum : ushort { }
    private enum UIntEnum : uint { }

    [Test]
    public void Test_Sizes_Correct()
    {
        Assert.AreEqual(1, typeof(byte).GetSize());
        Assert.AreEqual(1, typeof(ByteEnum).GetSize());
        Assert.AreEqual(1, typeof(sbyte).GetSize());
        Assert.AreEqual(2, typeof(short).GetSize());
        Assert.AreEqual(2, typeof(ushort).GetSize());
        Assert.AreEqual(2, typeof(UShortEnum).GetSize());
        Assert.AreEqual(4, typeof(int).GetSize());
        Assert.AreEqual(4, typeof(uint).GetSize());
        Assert.AreEqual(4, typeof(UIntEnum).GetSize());
        Assert.AreEqual(8, typeof(long).GetSize());
        Assert.AreEqual(8, typeof(ulong).GetSize());
        Assert.AreEqual(4, typeof(float).GetSize());
        Assert.AreEqual(8, typeof(double).GetSize());
        Assert.AreEqual(16, typeof(decimal).GetSize());
    }

    [Test]
    public void Test_Sizes_Async_Correct()
    {
        Parallel.For(0, 200, i =>
        {
            Task.Run(() => Assert.AreEqual(1, typeof(byte).GetSize()));
            Task.Run(() => Assert.AreEqual(1, typeof(ByteEnum).GetSize()));
            Task.Run(() => Assert.AreEqual(1, typeof(sbyte).GetSize()));
            Task.Run(() => Assert.AreEqual(2, typeof(short).GetSize()));
            Task.Run(() => Assert.AreEqual(2, typeof(ushort).GetSize()));
            Task.Run(() => Assert.AreEqual(2, typeof(UShortEnum).GetSize()));
            Task.Run(() => Assert.AreEqual(4, typeof(int).GetSize()));
            Task.Run(() => Assert.AreEqual(4, typeof(uint).GetSize()));
            Task.Run(() => Assert.AreEqual(4, typeof(UIntEnum).GetSize()));
            Task.Run(() => Assert.AreEqual(8, typeof(long).GetSize()));
            Task.Run(() => Assert.AreEqual(8, typeof(ulong).GetSize()));
            Task.Run(() => Assert.AreEqual(4, typeof(float).GetSize()));
            Task.Run(() => Assert.AreEqual(8, typeof(double).GetSize()));
            Task.Run(() => Assert.AreEqual(16, typeof(decimal).GetSize()));
        });
    }
}