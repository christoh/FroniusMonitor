namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecClient:IDisposable
{
    public bool IsConnected { get; }
    
    public Task ConnectAsync(string hostname, int port, ushort modbusAddress, TimeSpan timeout = default);
}
