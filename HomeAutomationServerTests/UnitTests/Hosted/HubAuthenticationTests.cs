using System.Net;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// Who gets onto the hub, over a real connection: a Kestrel host wired the way the server wires it, and real
/// SignalR clients that present a ticket, the wrong ticket, or none at all.
/// </summary>
public sealed class HubAuthenticationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly User member = TestUsers.Create("bob", Roles.User);
    private readonly User guest = TestUsers.Create("eve", Roles.Guest);
    private readonly SettableTimeProvider clock = new(Now);

    private WebApplication app = null!;
    private Uri hubUri = null!;
    private HubTicketService tickets = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Information);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IAesKeyProvider>(new TestAesKeyProvider());
        builder.Services.Configure<UserList>(list => list.Users = [member, guest]);

        // Registered before AddHubTicketAuthentication, which only adds TimeProvider.System if nobody else has.
        builder.Services.AddSingleton<TimeProvider>(clock);

        // AddAuthorization comes from AddControllers in the real server; this host has no controllers.
        builder.Services.AddAuthorization();

        builder.Services.AddHubTicketAuthentication();
        builder.Services.AddHomeAutomationSignalR();

        app = builder.Build();

        // The very registration and policy Program.cs uses, so neither can drift away from what is tested here.
        app.MapHub<DirectionProbeHub>("/hub").RequireAuthorization(policy => policy.RequireHubTicket());

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        hubUri = new Uri(new Uri(address), "hub");
        tickets = app.Services.GetRequiredService<HubTicketService>();
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task A_valid_ticket_gets_onto_the_hub()
    {
        await using var connection = Build(tickets.Issue(member));

        await connection.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task No_ticket_means_no_hub()
    {
        await using var connection = Build(ticket: null);

        Assert.Equal(HttpStatusCode.Unauthorized, await Refused(connection));
    }

    [Fact]
    public async Task A_ticket_from_another_server_is_not_accepted()
    {
        var elsewhere = new HubTicketService(new StrangerAesKeyProvider(), TestUsers.AsOptions(member), clock, NullLogger<HubTicketService>.Instance);
        await using var connection = Build(elsewhere.Issue(member));

        Assert.Equal(HttpStatusCode.Unauthorized, await Refused(connection));
    }

    [Fact]
    public async Task An_expired_ticket_is_not_accepted()
    {
        var ticket = tickets.Issue(member);
        clock.Now = Now + HubTicketService.Lifetime + TimeSpan.FromSeconds(1);

        await using var connection = Build(ticket);

        Assert.Equal(HttpStatusCode.Unauthorized, await Refused(connection));
    }

    [Fact]
    public async Task A_user_without_the_required_role_is_not_accepted()
    {
        // The ticket itself is perfectly valid - eve simply is not a User, and the policy on the hub asks for that.
        await using var connection = Build(tickets.Issue(guest));

        Assert.Equal(HttpStatusCode.Forbidden, await Refused(connection));
    }

    private HubConnection Build(string? ticket) => new HubConnectionBuilder()
        .WithUrl(hubUri, options =>
        {
            if (ticket != null)
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(ticket);
            }
        })
        .ConfigureLogging(l => l.AddTestOutput(LogLevel.Warning))
        .Build();

    /// <summary>The status the server answered the handshake with, having refused it.</summary>
    private static async Task<HttpStatusCode?> Refused(HubConnection connection)
    {
        var exception = await Assert.ThrowsAnyAsync<HttpRequestException>(() => connection.StartAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HubConnectionState.Disconnected, connection.State);
        return exception.StatusCode;
    }

    private sealed class StrangerAesKeyProvider : IAesKeyProvider
    {
        public byte[] GetAesKey() => [16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];
    }
}
