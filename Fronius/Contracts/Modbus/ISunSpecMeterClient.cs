namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecMeterClient : ISunSpecClient
{
    public Task<SunSpecMeter> GetDataAsync(CancellationToken token = default);
}
