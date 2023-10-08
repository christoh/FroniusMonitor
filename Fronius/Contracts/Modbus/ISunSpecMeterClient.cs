using De.Hochstaetter.Fronius.Models.Modbus;

namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecMeterClient : IDisposable
{
    public bool IsConnected { get; }
    
    public Task ConnectAsync(string hostname, int port, int modbusAddress, TimeSpan timeout = default);
    
    public Task<SunSpecMeter> GetDataAsync(CancellationToken token = default);
}
