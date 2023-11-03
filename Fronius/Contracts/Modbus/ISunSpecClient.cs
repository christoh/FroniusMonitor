namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecClient : IDisposable
{
    public bool IsConnected { get; }

    public Task ConnectAsync(string hostname, int port, byte modbusAddress, TimeSpan timeout = default);
    
    public Task<IList<SunSpecModelBase>> GetDataAsync(CancellationToken token = default);

    public Task WriteRegisters(SunSpecModelBase entity, params string[] propertyNames);
}
