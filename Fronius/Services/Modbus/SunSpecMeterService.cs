using FluentModbus;

namespace De.Hochstaetter.Fronius.Services.Modbus;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
public class SunSpecMeterService
{
    private readonly ILogger logger;
    private ModbusTcpServer? server;

    public SunSpecMeterService(ILogger<SunSpecMeterService> logger)
    {
        this.logger = logger;
    }

    public Task StartAsync() => Task.Run(() =>
    {
        server = new ModbusTcpServer(logger, true)
        {
            MaxConnections = 0,
        };

        //server.RequestValidator = (_, functionCode, address, numberOfRegisters) =>
        //{
        //    if (functionCode != ModbusFunctionCode.ReadHoldingRegisters)
        //    {
        //        return ModbusExceptionCode.IllegalFunction;
        //    }

        //    if (address is < 40000 or >= 50000)
        //    {
        //        return ModbusExceptionCode.IllegalDataAddress;
        //    }

        //    return ModbusExceptionCode.OK;
        //};

        server.Start(new IPEndPoint(IPAddress.Any, 502));
    });

    public void UpdateMeter(IPowerMeter1P meter, byte modbusAddress)
    {
        if (server == null)
        {
            throw new InvalidOperationException("Server not started");
        }

        lock (server.Lock)
        {
            if (!server.UnitIdentifiers.Contains(modbusAddress))
            {
                server.AddUnit(modbusAddress);
            }
            
            var registers = server.GetHoldingRegisters(modbusAddress);
            registers.WriteString(40000, 4, "SunS"); // magic
            registers.SetBigEndian<ushort>(40002, 1); // Common Model Block identifier
            registers.SetBigEndian<ushort>(40003, 65); // Length of Common Model Block
            registers.WriteString(40004, 32, meter.Manufacturer ?? nameof(meter.Manufacturer)); // manufacturer
            registers.WriteString(40020, 32, meter.DisplayName?? meter.Model ?? nameof(meter.Model)); // model
            registers.WriteString(40036, 16, "<secondary>"); // data manager version
            registers.WriteString(40044, 16, meter.DeviceVersion ?? "1.0.0-1"); // device version
            registers.WriteString(40052, 32, meter.SerialNumber ?? "0"); // serial number
            registers.SetBigEndian<ushort>(40068, modbusAddress); // Modbus address
            registers.SetBigEndian<ushort>(40069, 203); // single-phase with int+sf
            registers.SetBigEndian<ushort>(40070, 105); // length of data block (fixed)

            var scaleFactor = SetExponent(40076, -4);
            SetValue<short>(40072, meter.Current ?? 0);
            SetValue<short>(40073, meter.Current ?? 0);

            scaleFactor = SetExponent(40085, -1);
            SetValue<short>(40077, meter.Voltage ?? 0);
            SetValue<short>(40078, meter.Voltage ?? 0);
            SetValue<short>(40081, meter.Voltage ?? 0);
            SetValue<short>(40082, meter.Voltage ?? 0);

            scaleFactor = SetExponent(40087, -2);
            SetValue<short>(40086, meter.Frequency ?? 50);

            scaleFactor = SetExponent(40092, -2);
            SetValue<short>(40088, meter.PowerWatts ?? 0);
            SetValue<short>(40089, meter.PowerWatts ?? 0);

            scaleFactor = SetExponent(40097, -2);
            SetValue<short>(40093, meter.PowerWatts ?? 0);
            SetValue<short>(40094, meter.PowerWatts ?? 0);

            _ = SetExponent(40102, -2);
            scaleFactor = SetExponent(40107, -1);
            SetValue<short>(40103, 1);
            SetValue<short>(40104, 1);

            scaleFactor = SetExponent(40124, -3);
            SetValue<uint>(40116,(meter.EnergyKiloWattHours??0)*100);

            _ = SetExponent(40141, -32768);
            _ = SetExponent(40174, -32768);
            
            registers.SetBigEndian<short>(40176,-1);
            registers.SetBigEndian<short>(40177,0);

            double SetExponent(ushort register, short exponent)
            {
                var registers = server!.GetHoldingRegisters(modbusAddress);
                registers.SetBigEndian<short>(register - 1, exponent);
                return Math.Pow(10, exponent);
            }

            void SetValue<T>(ushort register, double value) where T : unmanaged, IConvertible
            {
                var intSfValue = (T)Convert.ChangeType(Math.Round(value / scaleFactor), typeof(T));
                var registers = server!.GetHoldingRegisters(modbusAddress);
                registers.SetBigEndian(register - 1, intSfValue);
            }
        }
    }
}
