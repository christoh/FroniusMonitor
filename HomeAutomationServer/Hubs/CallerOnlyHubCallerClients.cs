using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationServer.Hubs;

/// <summary>
/// An <see cref="IHubCallerClients"/> that only lets the caller be addressed. Every other proxy - all clients,
/// other clients, groups, users, foreign connection ids - is replaced by a <see cref="DiscardingClientProxy"/>
/// that drops whatever is handed to it.
/// </summary>
/// <remarks>
/// <see cref="ClientToServerOnlyHubFilter"/> puts this in front of <see cref="Hub.Clients"/> while a hub method
/// invoked by a client runs, so a client cannot make the server relay anything to another client. The server's own
/// messages go through <see cref="IHubContext{THub}"/> or a hub lifetime method and are never wrapped.
/// </remarks>
internal sealed class CallerOnlyHubCallerClients(
    IHubCallerClients inner,
    string callerConnectionId,
    string hubMethodName,
    ILogger logger
) : IHubCallerClients
{
    // Answering the caller is the server talking to a client, which is always allowed.
    public ISingleClientProxy Caller => inner.Caller;

    public ISingleClientProxy Client(string connectionId) => string.Equals(connectionId, callerConnectionId, StringComparison.Ordinal)
        ? inner.Caller
        : Discard($"Client(\"{connectionId}\")");

    public IClientProxy All => Discard("All");

    public IClientProxy Others => Discard("Others");

    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Discard("AllExcept");

    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Discard("Clients");

    public IClientProxy Group(string groupName) => Discard($"Group(\"{groupName}\")");

    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Discard("Groups");

    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Discard($"GroupExcept(\"{groupName}\")");

    public IClientProxy OthersInGroup(string groupName) => Discard($"OthersInGroup(\"{groupName}\")");

    public IClientProxy User(string userId) => Discard("User");

    public IClientProxy Users(IReadOnlyList<string> userIds) => Discard("Users");

    // IHubCallerClients redeclares these two as ISingleClientProxy; C# has no covariant return type for an
    // interface implementation, so the IClientProxy slots of the generic base interface are filled explicitly.
    IClientProxy IHubCallerClients<IClientProxy>.Caller => Caller;

    IClientProxy IHubClients<IClientProxy>.Client(string connectionId) => Client(connectionId);

    private DiscardingClientProxy Discard(string target) => new(hubMethodName, target, logger);
}

/// <summary>
/// A client proxy that goes nowhere. Fire and forget sends are dropped and logged; an invocation that expects an
/// answer throws, because no client will ever produce one.
/// </summary>
internal sealed class DiscardingClientProxy(string hubMethodName, string target, ILogger logger) : ISingleClientProxy
{
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "Discarded message '{Method}' that hub method '{HubMethod}' tried to send to {Target}: a client must not send messages to other clients",
                method, hubMethodName, target
            );
        }

        return Task.CompletedTask;
    }

    public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default) => throw new InvalidOperationException(
        $"Hub method '{hubMethodName}' cannot invoke '{method}' on {target}: a client must not send messages to other clients, so nobody can answer."
    );
}
