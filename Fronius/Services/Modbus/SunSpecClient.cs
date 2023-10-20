namespace De.Hochstaetter.Fronius.Services.Modbus;

public class SunSpecClient : SunSpecClientBase
{
    private readonly ILogger<SunSpecClient> logger;

    public SunSpecClient(ILogger<SunSpecClient> logger)
    {
        this.logger = logger;
    }

    public async Task GetDataAsync(CancellationToken token = default)
    {
        if (!IsConnected)
        {
            throw new SocketException((int)SocketError.NotConnected);
        }

        ushort register = 40000;
        var header = await ModbusClient!.ReadHoldingRegistersAsync<byte>(ModbusAddress, register, 4, token).ConfigureAwait(false);
        var magic = header.ReadString(0, 2);
        register += 2;
        var (sunSpecModel, data) = await ReadBlock(register, token).ConfigureAwait(false);
        register += (ushort)(2 + (data.Length >> 1));

        var manufacturer = data.ReadString(0, 16);
        var model = data.ReadString(16, 16);
        var options = data.ReadString(32, 8);
        var version = data.ReadString(40, 8);
        var serialNumber = data.ReadString(48, 16);
        var modbusAddress = data.Read<ushort>(64);

        for (; sunSpecModel != 0xffff; register += (ushort)(2 + (data.Length >> 1)))
        {
            (sunSpecModel, data) = await ReadBlock(register, token).ConfigureAwait(false);
        }
    }

    private async Task<(ushort Model, Memory<byte> Data)> ReadBlock(ushort register, CancellationToken token)
    {
        var header = await ModbusClient!.ReadHoldingRegistersAsync<byte>(ModbusAddress, register, 4, token).ConfigureAwait(false);
        var model = header.Read<ushort>(0);
        var length = header.Read<ushort>(1);

        if (length == 0)
        {
            return (model, new Memory<byte>());
        }
        
        var data = await ModbusClient!.ReadHoldingRegistersAsync<byte>(ModbusAddress, register + 2, length << 1, token).ConfigureAwait(false);
        return (model, data);
    }
}
