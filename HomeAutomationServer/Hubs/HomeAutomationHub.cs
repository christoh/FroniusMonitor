using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationServer.Hubs;

public class HomeAutomationHub() : Hub
{
    public async Task SendGen24Message(string id, string message, CancellationToken token = default)
    {
        await Clients.All.SendAsync("ReceiveMessage", id, message, token).ConfigureAwait(false);
    }
}