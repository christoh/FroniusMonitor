using FluentModbus;

namespace De.Hochstaetter.Fronius.Services.Modbus;

public class SunSpecClient : ISunSpecClient
{
    private static readonly ushort[] baseRegisters = { 40000, 50000 };
    private readonly ILogger<SunSpecClient> logger;
    private ushort baseRegister = 0xffff;

    public SunSpecClient(ILogger<SunSpecClient> logger)
    {
        this.logger = logger;
    }

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

            foreach (var register in baseRegisters)
            {
                try
                {
                    var id = await client.ReadStringAsync(modbusAddress, register, 4).ConfigureAwait(false);

                    if (id != "SunS")
                    {
                        continue;
                    }

                    baseRegister = register;
                    break;
                }
                catch (Exception e)
                {
                    continue;
                }
            }

            if (baseRegister == 0xffff)
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

    public async Task<IList<SunSpecModelBase>> GetDataAsync(CancellationToken token = default)
    {
        if (!IsConnected)
        {
            throw new SocketException((int)SocketError.NotConnected);
        }

        ushort register = baseRegister;
        var header = await ModbusClient!.ReadHoldingRegistersAsync<byte>(ModbusAddress, register, 4, token).ConfigureAwait(false);
        var magic = header.ReadString(0, 2);
        register += 2;
        Memory<byte> data;
        var result = new List<SunSpecModelBase>();

        for (ushort sunSpecModel = 0; sunSpecModel != 0xffff; register += (ushort)(2 + (data.Length >> 1)))
        {
            (sunSpecModel, data) = await ReadBlock(register, token).ConfigureAwait(false);

            var sunSpecModelBase = sunSpecModel switch
            {
                1 => new SunSpecCommonBlock(data, sunSpecModel, (ushort)(register + 2)),
                _ => null
            };

            if (sunSpecModelBase != null)
            {
                result.Add(sunSpecModelBase);
            }
        }

        return result;
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
