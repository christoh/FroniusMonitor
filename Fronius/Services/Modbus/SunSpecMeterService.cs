using FluentModbus;

namespace De.Hochstaetter.Fronius.Services.Modbus;

public class SunSpecMeterService
{
    private readonly ILogger logger;

    public SunSpecMeterService(ILogger<SunSpecMeterService> logger)
    {
        this.logger = logger;
    }

    public Task StartAsync() => Task.Run(() =>
    {
        var server = new ModbusTcpServer(logger, true)
        {
            MaxConnections = 0,
        };

        server.AddUnit(2);

        server.RequestValidator = (_, functionCode, address, numberOfRegisters) =>
        {
            if (functionCode != ModbusFunctionCode.ReadHoldingRegisters)
            {
                return ModbusExceptionCode.IllegalFunction;
            }

            if (address is < 40000 or >= 50000)
            {
                return ModbusExceptionCode.IllegalDataAddress;
            }

            return ModbusExceptionCode.OK;
        };

        lock (server.Lock)
        {
            var registers = server.GetHoldingRegisters(2);
            registers.WriteString(40000, 4, "SunS"); // magic
            registers.SetBigEndian<ushort>(40002, 1); // Common Model Block identifier
            registers.SetBigEndian<ushort>(40003, 65); // Length of Common Model Block
            registers.WriteString(40004, 32, "AVM"); // manufacturer
            registers.WriteString(40020, 32, "FRITZ!DECT 200"); // model
            registers.WriteString(40036, 16, "<secondary>"); // datamanager version
            registers.WriteString(40044, 16, "04.25"); // device version
            registers.WriteString(40052, 32, "116300301009"); // serial number
            registers.SetBigEndian<ushort>(40068, 2); // Modbus address
            registers.SetBigEndian<ushort>(40069, 201); // single-phase with int+sf
            registers.SetBigEndian<ushort>(40070,105); // length of data block (fixed)
        }

        server.Start(new IPEndPoint(IPAddress.Any, 1502));
    });
}
