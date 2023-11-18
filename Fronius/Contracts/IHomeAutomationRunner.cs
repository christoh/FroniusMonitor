namespace De.Hochstaetter.Fronius.Contracts;

internal interface IHomeAutomationRunner : IDisposable
{
    Task StartAsync(CancellationToken token = default);
    Task StopAsync(CancellationToken token = default);
}