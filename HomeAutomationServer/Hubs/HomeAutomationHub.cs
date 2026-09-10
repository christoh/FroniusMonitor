using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationServer.Hubs;

public class HomeAutomationHub(IDataControlService controlService, IWattPilotServices wattPilots, ILogger<HomeAutomationHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Client of {Username} connected on {ConnectionId}", Context.User?.Identity?.Name, Context.ConnectionId);
        }

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

    /// <summary>
    /// Writes a client's Wattpilot settings to the charger: whatever differs between <paramref name="wanted"/> and
    /// <paramref name="loaded"/>, one setValue per property, and waits for the charger to acknowledge each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Over the hub rather than a controller because that is the shape of the write: a Wattpilot write is a
    /// conversation on the socket the server already holds open - every setValue is answered later by a response
    /// carrying the resulting status, which then reaches every client as a <c>WattPilotUpdate</c> delta anyway.
    /// The answer to the caller is what the push channel does not carry: which writes failed or went unconfirmed.
    /// </para>
    /// <para>
    /// The difference is taken against <paramref name="loaded"/>, the state the client's dialog started from, and
    /// not against what the charger holds now: a value the charger changed on its own in the meantime is not
    /// something the user asked to change, and must not be written back to what it was.
    /// </para>
    /// <para>
    /// The same role as every Gen24 write endpoint asks for. The ticket that opened the connection only proves the
    /// User role, which is why this is said again here; the ticket principal carries the same role claims Basic
    /// authentication would.
    /// </para>
    /// </remarks>
    [Authorize(AuthenticationSchemes = HubTicketAuthenticationService.SchemeName, Roles = nameof(Roles.Operator))]
    public async Task<WattPilotWriteResult> SetWattPilotSettings(string id, WattPilot wanted, WattPilot loaded)
    {
        var service = FindService(id);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Username} writes settings to Wattpilot {Id}: {Properties}", Context.User?.Identity?.Name, id, string.Join(", ", wanted.ChangedSettings(loaded).Select(p => p.Name)));
        }

        var result = new WattPilotWriteResult();
        service.BeginSendValues();

        try
        {
            result.Errors = await service.Send(wanted, loaded).ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            // Nothing differed, so nothing was sent and there is nothing to wait for. The client checks this
            // before it calls; this is only the same answer for one that did not.
            return result;
        }

        try
        {
            await service.WaitSendValues().ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            result.Unconfirmed = [.. service.UnsuccessfulWrites.Select(acknowledge => acknowledge.ToString())];
        }

        return result;
    }

    [Authorize(AuthenticationSchemes = HubTicketAuthenticationService.SchemeName, Roles = nameof(Roles.Operator))]
    public async Task RebootWattPilot(string id)
    {
        var service = FindService(id);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Username} reboots Wattpilot {Id}", Context.User?.Identity?.Name, id);
        }

        await service.RebootWattPilot().ConfigureAwait(false);
    }

    /// <summary>
    /// A <see cref="HubException"/> is the one exception whose message reaches the caller; anything else arrives
    /// as a generic failure.
    /// </summary>
    private IWattPilotService FindService(string id) => wattPilots.Find(id) ?? throw new HubException(Resources.NoWattPilotConnection);
}
