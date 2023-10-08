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
}