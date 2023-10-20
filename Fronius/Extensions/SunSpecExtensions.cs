namespace De.Hochstaetter.Fronius.Extensions;

internal static class SunSpecExtensions
{
    public static string ReadString(this Memory<byte> data, ushort register, ushort length)
    {
        var range = data.Span.Slice(register << 1, length << 1);
        var nullIndex = range.IndexOf((byte)0);

        if (nullIndex >= 0)
        {
            range = range[0..nullIndex];
        }

        return Encoding.UTF8.GetString(range);
    }

    public static unsafe T Read<T>(this Memory<byte> data, ushort register) where T : unmanaged
    {
        var span = data.Span[(register << 1)..];

        if (BitConverter.IsLittleEndian)
        {
            long longData = 0;

            for (var i = 0; i < sizeof(T); i++)
            {
                longData <<= 8;
                longData |= span[i];
            }

            return *(T*)&longData;
        }

        fixed (byte* dataPointer = span)
        {
            return *(T*)(dataPointer);
        }
    }
}
