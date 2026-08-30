using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationServer.Hubs;

/// <summary>
/// Enforces the direction rules of the SignalR channel: the server may address any client, a client may only
/// address the server.
/// </summary>
/// <remarks>
/// While a hub method invoked by a client runs, <see cref="Hub.Clients"/> is swapped for a
/// <see cref="CallerOnlyHubCallerClients"/>, so every attempt to fan a client's message out to other clients is
/// discarded instead of delivered - whether the hub method meant to or not. The filter deliberately does not
/// implement <c>OnConnectedAsync</c> / <c>OnDisconnectedAsync</c>: those are server driven and keep the
/// unrestricted <see cref="Hub.Clients"/>, as does everything the server pushes through
/// <see cref="IHubContext{THub}"/>.
/// </remarks>
public sealed class ClientToServerOnlyHubFilter(ILogger<ClientToServerOnlyHubFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var hub = invocationContext.Hub;
        var unrestrictedClients = hub.Clients;

        hub.Clients = new CallerOnlyHubCallerClients(
            unrestrictedClients,
            invocationContext.Context.ConnectionId,
            invocationContext.HubMethodName,
            logger
        );

        try
        {
            return await next(invocationContext).ConfigureAwait(false);
        }
        finally
        {
            // SignalR creates a hub instance per invocation, so this only matters if that ever changes.
            hub.Clients = unrestrictedClients;
        }
    }
}
