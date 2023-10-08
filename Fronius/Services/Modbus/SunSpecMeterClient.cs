using System.Net.Sockets;
using De.Hochstaetter.Fronius.Contracts.Modbus;
using De.Hochstaetter.Fronius.Models.Modbus;
using FluentModbus;

namespace De.Hochstaetter.Fronius.Services.Modbus
{
    public class SunSpecMeterClient : ISunSpecMeterClient
    {
        private ModbusTcpClient? modbusClient;
        private int modbusAddress;

        public bool IsConnected => modbusClient is { IsConnected: true };

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public async Task ConnectAsync(string hostname, int port, int modbusAddress, TimeSpan timeout = default)
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

                modbusClient = client;
                this.modbusAddress = modbusAddress;
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

        public async Task<SunSpecMeter> GetDataAsync(CancellationToken token = default)
        {
            if (!IsConnected)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            var meter = new SunSpecMeter
            {
                Manufacturer = await modbusClient!.ReadStringAsync(modbusAddress, 40004, 32, token).ConfigureAwait(false),
                ModelName = await modbusClient!.ReadStringAsync(modbusAddress, 40020, 32, token).ConfigureAwait(false),
                DataManagerVersion = await modbusClient!.ReadStringAsync(modbusAddress, 40036, 16, token).ConfigureAwait(false),
                DeviceVersion = await modbusClient!.ReadStringAsync(modbusAddress, 40044, 16, token).ConfigureAwait(false),
                SerialNumber = await modbusClient!.ReadStringAsync(modbusAddress, 40052, 32, token).ConfigureAwait(false),
                ModbusAddress = (await modbusClient!.ReadHoldingRegistersAsync<ushort>(modbusAddress, 40068, 1, token).ConfigureAwait(false)).Span[0],
            };

            var dataHeader = await modbusClient!.ReadHoldingRegistersAsync<ushort>(modbusAddress, 40069, 2, token).ConfigureAwait(false);
            var deviceTypeAndDataFormat = dataHeader.Span[0];
            var dataLength = dataHeader.Span[1];
            meter.MeterType = (SunSpecMeterType)(deviceTypeAndDataFormat % 10);
            meter.Protocol = (SunSpecProtocol)(deviceTypeAndDataFormat / 10 % 10);

            switch (meter.Protocol)
            {
                case SunSpecProtocol.Float:
                    await ReadFloatData(meter, dataLength);
                    break;
                default:
                    throw new InvalidDataException("Unknown Sunspec protocol");
            }

            return meter;
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
                modbusClient?.Dispose();
                modbusClient = null;
                modbusAddress = -1;
            }
        }

        private async Task ReadFloatData(SunSpecMeter meter, [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")] ushort dataLength)
        {
            if (dataLength < 124)
            {
                throw new InvalidDataException("Need at least 124 registers");
            }
            
            var data = (await modbusClient!.ReadHoldingRegistersAsync<float>(modbusAddress, 40071, 61).ConfigureAwait(false)).ToArray();
            meter.TotalCurrent = data[0];
            meter.CurrentL1 = data[1];
            meter.CurrentL2 = data[2];
            meter.CurrentL3 = data[3];
            meter.PhaseVoltageAverage = data[4];
            meter.PhaseVoltageL1 = data[5];
            meter.PhaseVoltageL2 = data[6];
            meter.PhaseVoltageL3 = data[7];
            meter.LineVoltageAverage = data[8];
            meter.LineVoltageL12 = data[9];
            meter.LineVoltageL23 = data[10];
            meter.LineVoltageL31 = data[11];
        }
    }
}
