namespace De.Hochstaetter.Fronius.Extensions;

internal static class SunSpecExtensions
{
    public static string? ReadString(this Memory<byte> data, ushort register, ushort length)
    {
        var range = data.Span.Slice(register << 1, length << 1);
        var nullIndex = range.IndexOf((byte)0);

        if (nullIndex == 0)
        {
            return null;
        }

        if (nullIndex > 0)
        {
            range = range[0..nullIndex];
        }

        return Encoding.UTF8.GetString(range);
    }

    public static void WriteString(this Memory<byte> data, string? value, ushort register, ushort length)
    {
        var destination = data.Span.Slice(register << 1, length << 1);
        destination.Fill(0);

        if (value is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            bytes = bytes[..Math.Min(destination.Length, bytes.Length)];
            bytes.CopyTo(destination);
        }
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

    public static unsafe void Write<T>(this Memory<byte> data, ushort register, T value) where T : unmanaged
    {
        var span = data.Span[(register << 1)..];

        if (BitConverter.IsLittleEndian)
        {
            byte* valuePointer = (byte*)&value;
            var swappedValuePointer = stackalloc byte[sizeof(T)];

            for (var i = 0; i < sizeof(T); i++)
            {
                swappedValuePointer[sizeof(T) - i - 1] = valuePointer[i];
            }

            value = *(T*)swappedValuePointer;
        }

        fixed (byte* dataPointer = span)
        {
            *(T*)(dataPointer) = value;
        }
    }
}
