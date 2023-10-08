using FluentModbus;

namespace De.Hochstaetter.Fronius.Services.Modbus;

public class SunSpecClientBase : ISunSpecClient
{
    protected ModbusTcpClient? ModbusClient { get; private set; }
    protected int ModbusAddress { get; private set; } = ~0;

    public bool IsConnected => ModbusClient is { IsConnected: true };

    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    public async Task ConnectAsync(string hostname, int port, ushort modbusAddress, TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = TimeSpan.FromSeconds(5);
        }

        var ipAddresses = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false) ?? throw new SocketException((int)SocketError.HostNotFound);
        var client = new ModbusTcpClient();
        Exception? exception = null;

        foreach (var ipAddress in ipAddresses)
        {
            client.ConnectTimeout = (int)timeout.TotalMilliseconds;
            client.ReadTimeout = 2000;

            try
            {
                await Task.Run(() => client.Connect(new IPEndPoint(ipAddress, port), ModbusEndianness.BigEndian)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
                continue;
            }

            ModbusClient = client;
            ModbusAddress = modbusAddress;
            var id = await client.ReadStringAsync(modbusAddress, 40000, 4).ConfigureAwait(false);

            if
            (
                id != "SunS" ||
                (await client.ReadHoldingRegistersAsync<ushort>(modbusAddress, 40002, 1)).Span[0] != 1 ||
                (await client.ReadHoldingRegistersAsync<ushort>(modbusAddress, 40003, 1)).Span[0] != 65
            )
            {
                throw new InvalidDataException("Not a SunSpec device");
            }

            return;
        }

        throw exception ?? new SocketException((int)SocketError.HostNotFound);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ModbusClient?.Dispose();
            ModbusClient = null;
            ModbusAddress = ~0;
        }
    }

    protected async Task<(T Device, ushort DeviceTypeAndDataFormat, ushort numberOfRegisters, ReadOnlyMemory<byte> Data)> CreateSunSpecDevice<T>(CancellationToken token) where T : SunSpecDeviceBase, new()
    {
        var specDeviceBase = new T
        {
            Manufacturer = await ModbusClient!.ReadStringAsync(ModbusAddress, 40004, 32, token).ConfigureAwait(false),
            ModelName = await ModbusClient!.ReadStringAsync(ModbusAddress, 40020, 32, token).ConfigureAwait(false),
            DataManagerVersion = await ModbusClient!.ReadStringAsync(ModbusAddress, 40036, 16, token).ConfigureAwait(false),
            DeviceVersion = await ModbusClient!.ReadStringAsync(ModbusAddress, 40044, 16, token).ConfigureAwait(false),
            SerialNumber = await ModbusClient!.ReadStringAsync(ModbusAddress, 40052, 32, token).ConfigureAwait(false),
            ModbusAddress = (await ModbusClient!.ReadHoldingRegistersAsync<ushort>(ModbusAddress, 40068, 1, token).ConfigureAwait(false)).Span[0],
        };

        ReadOnlyMemory<ushort> dataHeader = await ModbusClient!.ReadHoldingRegistersAsync<ushort>(ModbusAddress, 40069, 2, token).ConfigureAwait(false);
        var deviceTypeAndDataFormat = dataHeader.Span[0];
        var numberOfRegisters = dataHeader.Span[1];
        ReadOnlyMemory<byte> data = await GetData(numberOfRegisters).ConfigureAwait(false);
        return (specDeviceBase, deviceTypeAndDataFormat, numberOfRegisters, data);
    }

    [Pure]
    protected static unsafe T ReadRelative<T>(ReadOnlyMemory<byte> data, ushort position) where T : unmanaged
    {
        var span = data.Span;

        if (BitConverter.IsLittleEndian)
        {
            long longData = 0;

            for (var i = position; i < position + sizeof(T); i++)
            {
                longData <<= 8;
                longData |= span[i];
            }

            return *(T*)&longData;
        }

        fixed (byte* dataPointer = span)
        {
            return *(T*)(dataPointer + position);
        }
    }

    private Task<Memory<byte>> GetData(ushort numberOfRegisters)
    {
        return ModbusClient!.ReadHoldingRegistersAsync<byte>(ModbusAddress, 40071, numberOfRegisters << 1);
    }
}
