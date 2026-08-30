using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// The direction rules over a real connection: a Kestrel host set up with the very registration the server uses
/// (<see cref="SignalRRegistration.AddHomeAutomationSignalR"/>) and two real SignalR clients talking to it.
/// </summary>
public sealed class HubMessageDirectionTests : IAsyncLifetime
{
    private WebApplication app = null!;
    private Uri hubUri = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Information);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddHomeAutomationSignalR();

        app = builder.Build();
        app.MapHub<DirectionProbeHub>("/hub");
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        hubUri = new Uri(new Uri(address), "hub");
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task The_server_reaches_every_client()
    {
        await using var a = await Connect();
        await using var b = await Connect();

        // Both were greeted from OnConnectedAsync, which is where the real hub replays its device list.
        Assert.True(a.Got(DirectionProbeHub.Welcome));
        Assert.True(b.Got(DirectionProbeHub.Welcome));

        await PushFromServer();

        Assert.Equal("from the server", await a.WaitFor(DirectionProbeHub.ServerPush));
        Assert.Equal("from the server", await b.WaitFor(DirectionProbeHub.ServerPush));
    }

    [Fact]
    public async Task A_client_cannot_make_the_server_broadcast()
    {
        await using var a = await Connect();
        await using var b = await Connect();

        await a.Connection.InvokeAsync(nameof(DirectionProbeHub.FanOut), TestContext.Current.CancellationToken);
        await SettleAndWaitFor(a, b);

        Assert.False(b.Got(DirectionProbeHub.Broadcast));
        Assert.False(a.Got(DirectionProbeHub.Broadcast));

        // The caller may still be answered - that is the server talking back, not a relay.
        Assert.True(a.Got(DirectionProbeHub.Direct));
        Assert.False(b.Got(DirectionProbeHub.Direct));
    }

    [Fact]
    public async Task A_client_cannot_address_another_connection_by_its_id()
    {
        await using var a = await Connect();
        await using var b = await Connect();

        await a.Connection.InvokeAsync(nameof(DirectionProbeHub.SendToConnection), b.ConnectionId, TestContext.Current.CancellationToken);
        await SettleAndWaitFor(a, b);

        Assert.False(b.Got(DirectionProbeHub.Broadcast));
    }

    [Fact]
    public async Task A_client_cannot_invoke_another_client()
    {
        await using var a = await Connect();
        await using var b = await Connect();

        await Assert.ThrowsAsync<HubException>(
            () => a.Connection.InvokeAsync<string>(nameof(DirectionProbeHub.AskConnection), b.ConnectionId, TestContext.Current.CancellationToken)
        );

        await SettleAndWaitFor(a, b);
        Assert.False(b.Got(DirectionProbeHub.Question));
    }

    private Task<ProbeClient> Connect() => ProbeClient.ConnectAsync(hubUri, TestContext.Current.CancellationToken);

    private Task PushFromServer() => app.Services.GetRequiredService<IHubContext<DirectionProbeHub>>()
        .Clients.All.SendAsync(DirectionProbeHub.ServerPush, "from the server", TestContext.Current.CancellationToken);

    /// <summary>
    /// Waiting for a fixed time would only say "nothing arrived yet". Sending something from the server and
    /// waiting for that instead proves the connections are alive and that anything the hub method sent has had
    /// its chance to arrive first, because SignalR keeps the order per connection.
    /// </summary>
    private async Task SettleAndWaitFor(params ProbeClient[] clients)
    {
        await PushFromServer();

        foreach (var client in clients)
        {
            await client.WaitFor(DirectionProbeHub.ServerPush);
        }
    }
}
