using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationServer.Hubs;

public class HomeAutomationHub(IDataControlService controlService, ILogger<HomeAutomationHub> logger) : Hub
{

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);
        logger.LogInformation("Client connected");

        foreach (var e in controlService.Entities)
        {
            await Clients.Caller.SendAsync(e.Value.Device.GetType().Name, e.Key, e.Value.Device).ConfigureAwait(false);
        }
    }

    public async Task SendGen24Message(string id, string message, CancellationToken token = default)
    {
        await Clients.All.SendAsync("ReceiveMessage", id, message, token).ConfigureAwait(false);
    }
}