namespace De.Hochstaetter.Fronius.Contracts;

internal interface IDataCollector : IAsyncDisposable
{
    Task StartAsync(CancellationToken token = default);
    Task StopAsync(CancellationToken token = default);
}