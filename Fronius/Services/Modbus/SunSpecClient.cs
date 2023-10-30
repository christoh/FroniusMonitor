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

    public async Task ConnectAsync(string hostname, int port, ushort modbusAddress, TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = TimeSpan.FromSeconds(5);
        }

        var ipAddresses = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false) ?? throw new SocketException((int)SocketError.HostNotFound);
        logger.LogTrace("IP addresses found: {IpAddresses}", string.Join(", ", ipAddresses.Select(i => i.ToString())));
        var client = new ModbusTcpClient();
        Exception? exception = null;

        foreach (var ipAddress in ipAddresses)
        {
            client.ConnectTimeout = (int)timeout.TotalMilliseconds;
            client.ReadTimeout = 2000;

            try
            {
                logger.LogTrace("Connecting to {IpAddress}:{port}", ipAddress, port);
                await Task.Run(() => client.Connect(new IPEndPoint(ipAddress, port), ModbusEndianness.BigEndian)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Connection to {IpAddress}:{port} failed", ipAddress, port);
                exception = ex;
                continue;
            }

            ModbusClient = client;
            ModbusAddress = modbusAddress;

            foreach (var register in baseRegisters)
            {
                try
                {
                    logger.LogTrace("Reading from modbus register {ModbusRegister}", register);
                    var id = await client.ReadStringAsync(modbusAddress, register, 4).ConfigureAwait(false);

                    if (id != "SunS")
                    {
                        logger.LogTrace("Register {ModbusRegister} has no SunSpec magic", register);
                        continue;
                    }

                    baseRegister = register;
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogTrace(ex, "Could not read 2 registers starting at {ModbusRegister}", register);
                }
            }

            if (baseRegister == 0xffff)
            {
                logger.LogError("Modbus address {ModbusAddress} is not a SunSpec device", modbusAddress);
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
        register += 2;
        Memory<byte> data;
        var result = new List<SunSpecModelBase>();

        for (ushort sunSpecModel = 0; sunSpecModel != 0xffff; register += (ushort)(2 + (data.Length >> 1)))
        {
            (sunSpecModel, data) = await ReadBlock(register, token).ConfigureAwait(false);

            var sunSpecModelBase = sunSpecModel switch
            {
                1 => (SunSpecModelBase)new SunSpecCommonBlock(data, sunSpecModel, (ushort)(register + 2)),
                >= 101 and <= 103 => new SunSpecInverterInt(data, sunSpecModel, (ushort)(register + 2)),
                >= 111 and <= 113 => new SunSpecInverterFloat(data, sunSpecModel, (ushort)(register + 2)),
                120 => new SunSpecNamePlate(data, sunSpecModel, (ushort)(register + 2)),
                121 => new SunSpecInverterBasicSettings(data, sunSpecModel, (ushort)(register + 2)),
                122 => new SunSpecInverterExtendedMeasurements(data, sunSpecModel, (ushort)(register + 2)),
                123 => new SunSpecInverterControls(data, sunSpecModel, (ushort)(register + 2)),
                160 => new SunSpecMultipleMppt(data, sunSpecModel, (ushort)(register + 2)),
                >= 201 and <= 204 => new SunSpecMeterInt(data, sunSpecModel, (ushort)(register + 2)),
                >= 211 and <= 214 => new SunSpecMeterFloat(data, sunSpecModel, (ushort)(register + 2)),
                _ => null
            };

            if (sunSpecModelBase != null)
            {
                result.Add(sunSpecModelBase);
                logger.LogTrace("Added {ModelBase} to SunSpec group", sunSpecModelBase.GetType().Name);
            }

            if (sunSpecModelBase == null && sunSpecModel != 0xffff)
            {
                logger.LogWarning("Unknown SunSpecModel {ModelNumber} received", sunSpecModel);
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
