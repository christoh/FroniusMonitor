namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecMeterClient : ISunSpecClient
{
    public Task<SunSpecMeterOld> GetDataAsync(CancellationToken token = default);
}
