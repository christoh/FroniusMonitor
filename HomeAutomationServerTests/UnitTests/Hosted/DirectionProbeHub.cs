namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// A hub that tries every way of reaching another client, so the tests can prove that none of them gets through.
/// It stands in for <see cref="HomeAutomationHub"/>, which needs the whole device stack to run.
/// </summary>
public class DirectionProbeHub : Hub
{
    public const string Welcome = nameof(Welcome);
    public const string Broadcast = nameof(Broadcast);
    public const string Direct = nameof(Direct);
    public const string ServerPush = nameof(ServerPush);
    public const string Question = nameof(Question);

    /// <summary>The server greeting a fresh connection - the same thing the real hub does with the device list.</summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);
        await Clients.Caller.SendAsync(Welcome, Context.ConnectionId).ConfigureAwait(false);
    }

    /// <summary>Everything here but the last line is a client trying to reach other clients.</summary>
    public async Task FanOut()
    {
        await Clients.All.SendAsync(Broadcast, "to all").ConfigureAwait(false);
        await Clients.Others.SendAsync(Broadcast, "to others").ConfigureAwait(false);
        await Clients.AllExcept([]).SendAsync(Broadcast, "to all except nobody").ConfigureAwait(false);
        await Clients.Group("any").SendAsync(Broadcast, "to a group").ConfigureAwait(false);
        await Clients.User("anybody").SendAsync(Broadcast, "to a user").ConfigureAwait(false);
        await Clients.Caller.SendAsync(Direct, "to the caller").ConfigureAwait(false);
    }

    public async Task SendToConnection(string connectionId)
    {
        await Clients.Client(connectionId).SendAsync(Broadcast, "to one connection").ConfigureAwait(false);
    }

    public async Task<string> AskConnection(string connectionId)
    {
        return await Clients.Client(connectionId).InvokeAsync<string>(Question, Context.ConnectionAborted).ConfigureAwait(false);
    }
}
