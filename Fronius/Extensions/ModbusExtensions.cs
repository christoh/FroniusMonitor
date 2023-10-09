using FluentModbus;

namespace De.Hochstaetter.Fronius.Extensions;

public static class ModbusExtensions
{
    public static async Task<string> ReadStringAsync(this ModbusClient client, int modbusAddress, int register, int length, CancellationToken token = default)
    {
        var bytes = await client.ReadHoldingRegistersAsync<byte>(modbusAddress, register, length, token).ConfigureAwait(false);
        var index = bytes.Span.IndexOf((byte)0);
        return Encoding.UTF8.GetString(bytes.Span[..(index < 0 ? bytes.Length : index)]);
    }

    public static unsafe void WriteString(this Span<short> registers, ushort register, ushort length, string text, bool omitLength = true)
    {
        if ((length & 1) == 1)
        {
            throw new ArgumentException($"{nameof(length)} must be an even number");
        }

        if (text.Length > length)
        {
            throw new ArgumentException($"{nameof(text)} is too long");
        }

        if (!omitLength)
        {
            registers.SetBigEndian<ushort>(register++, (ushort)(length >> 1));
        }

        var bytes = Encoding.ASCII.GetBytes(text);

        fixed (short* registerPointer = registers[register..])
        fixed (byte* bytePointer = bytes)
        {
            Buffer.MemoryCopy(bytePointer, registerPointer, registers.Length - register, bytes.Length);
        }

        //    for (var i = 0; i < bytes.Length >> 1; i++)
        //    {
        //        registers[register + i] = unchecked((short)((bytes[i << 1] << 8) | (bytes[(i << 1) + 1])));
        //    }
        //}
    }
}
