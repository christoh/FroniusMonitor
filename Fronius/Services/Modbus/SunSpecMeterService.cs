using De.Hochstaetter.Fronius.Models.Events;
using FluentModbus;

namespace De.Hochstaetter.Fronius.Services.Modbus;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
public class SunSpecMeterService
{
    private readonly ILogger<SunSpecMeterService> logger;
    private readonly IDataControlService dataControlService;
    private readonly IEnumerable<ModbusMapping> mappings;
    private ModbusTcpServer? server;

    public SunSpecMeterService(IEnumerable<ModbusMapping> mappings, ILogger<SunSpecMeterService> logger, IDataControlService dataControlService)
    {
        this.logger = logger;
        this.dataControlService = dataControlService;
        this.mappings = mappings;
    }

    public Task StartAsync(IPEndPoint endPoint) => Task.Run(() =>
    {
        dataControlService.DeviceUpdate += OnDeviceUpdate;

        server = new ModbusTcpServer(logger, true)
        {
            MaxConnections = 0,
            RequestValidator = (_, functionCode, address, numberOfRegisters) =>
            {
                if (functionCode != ModbusFunctionCode.ReadHoldingRegisters)
                {
                    return ModbusExceptionCode.IllegalFunction;
                }

                if (address < 40000 || address + numberOfRegisters > 49999)
                {
                    return ModbusExceptionCode.IllegalDataAddress;
                }

                return ModbusExceptionCode.OK;
            }
        };

        server.Start(endPoint);
    });

    private void OnDeviceUpdate(object? s, DeviceUpdateEventArgs e)
    {
        if (e.Device is not IPowerMeter1P { CanMeasurePower: true } meter)
        {
            return;
        }

        var mapping = mappings.SingleOrDefault(m => string.Equals(m.SerialNumber, meter.SerialNumber, StringComparison.Ordinal));

        if (mapping == null)
        {
            logger.LogWarning("Power meter {DisplayName} ({SerialNumber}) has no Modbus mapping", meter.DisplayName, meter.SerialNumber);
            return;
        }

        UpdateMeter(meter, mapping, e.DeviceAction);
    }

    public void UpdateMeter(IPowerMeter1P meter, ModbusMapping mapping, DeviceAction deviceAction)
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
                server.RemoveUnit(mapping.ModbusAddress);
                logger.LogInformation("Removing power meter {DisplayName} at modbus address {ModbusAddress}", meter.DisplayName, mapping.ModbusAddress);
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

                registers.SetBigEndian<short>(absoluteRegister++, -1);
                registers.SetBigEndian<short>(absoluteRegister, 0);
            }
        }
    }
}
