namespace De.Hochstaetter.Fronius.Contracts;

public interface IHomeAutomationRunner : IDisposable
{
    Task StartAsync(CancellationToken token = default);
    Task StopAsync(CancellationToken token = default);
}