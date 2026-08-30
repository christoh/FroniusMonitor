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

    /// <summary>
    /// The client to server lane: a client hands a Gen24 message to the server and the server keeps it.
    /// </summary>
    /// <remarks>
    /// This used to relay the message to <c>Clients.All</c>, which let any client push arbitrary data to every
    /// other client. Whatever the server is meant to do with such a message belongs here; it must never be sent on
    /// to other clients. <see cref="ClientToServerOnlyHubFilter"/> would discard that anyway.
    /// </remarks>
    public Task SendGen24Message(string id, string message, CancellationToken token = default)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Received Gen24 message for {Id} from connection {ConnectionId}: {Message}", id, Context.ConnectionId, message);
        }

        return Task.CompletedTask;
    }
}
