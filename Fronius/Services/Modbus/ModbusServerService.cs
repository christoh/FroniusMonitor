using De.Hochstaetter.Fronius.Models.Events;
using FluentModbus;

namespace De.Hochstaetter.Fronius.Services.Modbus;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
public class ModbusServerService : IHomeAutomationRunner
{
    private readonly ILogger<ModbusServerService> logger;
    private readonly IDataControlService dataControlService;
    private readonly IOptionsMonitor<ModbusServerServiceParameters> options;
    private readonly SettingsChangeTracker tracker;
    private ModbusTcpServer? server;
    private ushort maxAddress;

    public ModbusServerService
    (
        SettingsChangeTracker tracker,
        ILogger<ModbusServerService> logger,
        IDataControlService dataControlService,
        IOptionsMonitor<ModbusServerServiceParameters> options
    )
    {
        this.logger = logger;
        this.dataControlService = dataControlService;
        this.options = options;
        this.tracker = tracker;
    }

    private ModbusServerServiceParameters Parameters => options.CurrentValue ?? throw new ArgumentNullException(nameof(options), @$"{nameof(options)} are not configured");

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);

        await Task.Run(() =>
        {
            server = new ModbusTcpServer(logger, true)
            {
                MaxConnections = 0,

                RequestValidator = (modbusAddress, functionCode, startingRegister, numberOfRegisters) =>
                {
                    if (functionCode != ModbusFunctionCode.ReadHoldingRegisters)
                    {
                        logger.LogWarning("Client requested {FunctionCode} for address {ModbusAddress}: {ModbusExceptionCode}", functionCode, modbusAddress, ModbusExceptionCode.IllegalFunction);
                        return ModbusExceptionCode.IllegalFunction;
                    }

                    if (startingRegister < 40000 || startingRegister + numberOfRegisters > maxAddress)
                    {
                        logger.LogWarning
                        (
                            "Client tried to read {NumberOfRegisters} register(s) starting at {StartingRegister} from address {ModbusAddress}: {ModbusExceptionCode}",
                            numberOfRegisters,
                            startingRegister,
                            modbusAddress,
                            ModbusExceptionCode.IllegalDataAddress
                        );

                        return ModbusExceptionCode.IllegalDataAddress;
                    }

                    return ModbusExceptionCode.OK;
                }
            };

            logger.LogInformation
            (
                "Starting server on {IpAddress}:{Port}",
                Parameters.EndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{Parameters.EndPoint.Address}]" : Parameters.EndPoint.Address,
                Parameters.EndPoint.Port
            );

            server.Start(new ModbusTcpClientProvider(Parameters.EndPoint));
            dataControlService.DeviceUpdate += OnDeviceUpdate;
        }, token).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken token = default) => Task.Run(() =>
    {
        dataControlService.DeviceUpdate -= OnDeviceUpdate;
        server?.Dispose();
        server = null;
    }, token);

    //TODO: Extend to handle 3 phase power meters and probably other devices
    private void OnDeviceUpdate(object? s, DeviceUpdateEventArgs e)
    {
        if (e.Device is not IPowerMeter1P { CanMeasurePower: true } meter)
        {
            return;
        }

        var mapping = Parameters.Mappings.SingleOrDefault(m => string.Equals(m.SerialNumber, meter.SerialNumber, StringComparison.Ordinal));

        if (mapping == null)
        {
            if (!Parameters.AutoMap)
            {
                logger.LogWarning("Power meter {DisplayName} ({SerialNumber}) has no Modbus mapping", meter.DisplayName, meter.SerialNumber);
                return;
            }

            if (string.IsNullOrWhiteSpace(meter.SerialNumber))
            {
                logger.LogError("Cannot auto map add {DisplayName} ({SerialNumber}). Serial number is null or empty", meter.DisplayName, meter.SerialNumber);
                return;
            }

            byte modbusAddress = 2;

            for (; modbusAddress < byte.MaxValue; modbusAddress++)
            {
                if (modbusAddress is < 15 or > 83 && !Parameters.Mappings.Select(m => m.ModbusAddress).Contains(modbusAddress))
                {
                    break;
                }
            }

            mapping = new() { ModbusAddress = modbusAddress, Phase = 1, SerialNumber = meter.SerialNumber };
            Parameters.Mappings.Add(mapping);
            tracker.NotifySettingsChanged(options);
            logger.LogInformation("Auto mapped {DisplayName} ({SerialNumber}) to modbus address {ModbusAddress}", meter.DisplayName, meter.SerialNumber, mapping.ModbusAddress);
        }

        UpdateMeter(meter, mapping, e.DeviceAction);
    }

    private void UpdateMeter(IPowerMeter1P meter, ModbusMapping mapping, DeviceAction deviceAction)
    {
        if (server == null)
        {
            throw new InvalidOperationException("Server not started");
        }

        var sunSpecCommonBlock = new SunSpecCommonBlock(0)
        {
            ModbusAddress = mapping.ModbusAddress,
            SerialNumber = meter.SerialNumber,
            Manufacturer = meter.Manufacturer,
            ModelName = meter.Model,
            Options = "<secondary>",
            Version = meter.DeviceVersion
        };

        var sunSpecMeter = new SunSpecMeterFloat(213, 0)
        {
            TotalCurrent = meter.Current,
            PhaseVoltageAverage = meter.Voltage,
            Frequency = 50,
            ActivePowerSum = meter.ActivePower,
            EnergyActiveProduced = 0,
            EnergyActiveConsumed = meter.EnergyConsumed
        };

        logger.LogTrace("{DeviceAction}: {DisplayName}: {Power} W / {Voltage} V / {Current} A / {Energy} Wh", deviceAction, meter.DisplayName, meter.ActivePower, meter.Voltage, meter.Current, meter.EnergyConsumed);

        switch (mapping.Phase)
        {
            case 1:
                sunSpecMeter.CurrentL1 = meter.Current;
                sunSpecMeter.PhaseVoltageL1 = meter.Voltage;
                sunSpecMeter.ActivePowerL1 = meter.ActivePower;
                break;

            case 2:
                sunSpecMeter.CurrentL2 = meter.Current;
                sunSpecMeter.PhaseVoltageL2 = meter.Voltage;
                sunSpecMeter.ActivePowerL2 = meter.ActivePower;
                break;

            case 3:
                sunSpecMeter.CurrentL3 = meter.Current;
                sunSpecMeter.PhaseVoltageL3 = meter.Voltage;
                sunSpecMeter.ActivePowerL3 = meter.ActivePower;
                break;
        }

        lock (server.Lock)
        {
            if (deviceAction == DeviceAction.Delete)
            {
                logger.LogInformation("Removing power meter {DisplayName} at modbus address {ModbusAddress}", meter.DisplayName, mapping.ModbusAddress);
                server.RemoveUnit(mapping.ModbusAddress);
                return;
            }

            if (deviceAction == DeviceAction.Add)
            {
                logger.LogInformation("Adding power meter {DisplayName} at modbus address {ModbusAddress}", meter.DisplayName, mapping.ModbusAddress);
                server.AddUnit(mapping.ModbusAddress);
            }

            WriteModbus(sunSpecCommonBlock, sunSpecMeter);
            return;

            unsafe void WriteModbus(params SunSpecModelBase[] models)
            {
                var registers = server.GetHoldingRegisters(mapping.ModbusAddress);
                registers.WriteString(40000, 2, "SunS");
                ushort absoluteRegister = 40002;

                foreach (var model in models)
                {
                    registers.SetBigEndian(absoluteRegister++, model.ModelNumber);
                    registers.SetBigEndian(absoluteRegister++, model.DataLength);

                    fixed (short* registerPointer = registers[absoluteRegister..])
                    fixed (byte* dataPointer = model.RawData.Span)
                    {
                        Buffer.MemoryCopy(dataPointer, registerPointer, registers[absoluteRegister..].Length, model.RawData.Length);
                    }

                    absoluteRegister += model.DataLength;
                }

                registers.SetBigEndian<ushort>(absoluteRegister++, 0xffff);
                registers.SetBigEndian<short>(absoluteRegister++, 0);
                maxAddress = absoluteRegister;
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not stop {Service}", GetType().Name);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
