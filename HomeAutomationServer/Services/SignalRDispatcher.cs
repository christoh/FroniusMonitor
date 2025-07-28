using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.HomeAutomationServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationServer.Services;

public sealed class SignalRDispatcher(IHubContext<HomeAutomationHub> hubContext, IDataControlService controlService) : IHomeAutomationRunner
{
    public void Dispose()
    {
        controlService.DeviceUpdate -= OnDeviceUpdate;
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        controlService.DeviceUpdate += OnDeviceUpdate;
    }

    public Task StopAsync(CancellationToken token = default)
    {
        controlService.DeviceUpdate -= OnDeviceUpdate;
        return Task.CompletedTask;
    }

    private void OnDeviceUpdate(object? sender, DeviceUpdateEventArgs e)
    {
        if (e.DeviceAction is DeviceAction.Add or DeviceAction.Change)
        {
            _ = hubContext.Clients.All.SendAsync("DeviceUpdate", e.Device.Device.GetType().Name, e.Id, e.Device.Device).ConfigureAwait(false);
        }
    }
}
