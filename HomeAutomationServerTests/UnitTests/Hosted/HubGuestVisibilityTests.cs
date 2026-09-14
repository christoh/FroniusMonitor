using System.Collections.Concurrent;
using System.Text.Json;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.HomeAutomationServer.Hubs;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// What a guest is sent over the hub, against the real <see cref="HomeAutomationHub"/> and the real
/// <see cref="SignalRDispatcher"/> on a real connection: the inverters, on the greeting and on every push, and
/// never an outlet. A user beside them gets both.
/// </summary>
/// <remarks>
/// The negative assertions - "the guest never got the outlet" - are kept honest without sleeping: the test waits
/// for a later message the guest does receive, and SignalR keeps the order per connection, so anything sent before
/// it has had its chance to arrive.
/// </remarks>
public sealed class HubGuestVisibilityTests : IAsyncLifetime
{
    private const string InverterId = "Fronius;Symo_GEN24;12345";
    private const string OutletId = "AVM;FRITZ!DECT_200;98765";

    private readonly User member = TestUsers.Create("bob", Roles.User);
    private readonly User guest = TestUsers.Create("eve", Roles.Guest);
    private readonly DataControlService controlService = new(NullLogger<DataControlService>.Instance);

    private WebApplication app = null!;
    private Uri hubUri = null!;
    private HubTicketService tickets = null!;

    private sealed class NoWattPilots : IWattPilotServices
    {
        public IWattPilotService? Find(string deviceId) => null;
    }

    public async ValueTask InitializeAsync()
    {
        controlService.AddOrUpdate(InverterId, new ManagedDevice(new Gen24System(), null, typeof(IGen24Service)));
        controlService.AddOrUpdate(OutletId, new ManagedDevice(new FritzBoxDevice { Ain = "11657 0240192" }, null, typeof(IFritzBoxService)));

        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Information);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IAesKeyProvider>(new TestAesKeyProvider());
        builder.Services.Configure<UserList>(list => list.Users = [member, guest]);
        builder.Services.AddAuthorization();
        builder.Services.AddHubTicketAuthentication();
        builder.Services.AddHomeAutomationSignalR();

        // The real hub and the real dispatcher over the device list the collectors would fill.
        builder.Services.AddSingleton<IDataControlService>(controlService);
        builder.Services.AddSingleton<IWattPilotServices>(new NoWattPilots());
        builder.Services.AddSingleton<IToshibaHvacService>(new FakeToshibaHvacService());
        builder.Services.AddSingleton<SignalRDispatcher>();

        app = builder.Build();
        app.MapHub<HomeAutomationHub>("/hub").RequireAuthorization(policy => policy.RequireHubTicket());
        await app.StartAsync();
        await app.Services.GetRequiredService<SignalRDispatcher>().StartAsync(TestContext.Current.CancellationToken);

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        hubUri = new Uri(new Uri(address), "hub");
        tickets = app.Services.GetRequiredService<HubTicketService>();
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task A_guest_is_greeted_with_the_inverters_and_not_with_the_outlets()
    {
        await using var eve = await Connect(guest);

        await eve.WaitFor(nameof(Gen24System), 1);

        // Something the guest does get, sent after the greeting: once it is here, the greeting is complete.
        controlService.AddOrUpdate(InverterId, new ManagedDevice(new Gen24System(), null, typeof(IGen24Service)));
        await eve.WaitFor(nameof(Gen24System), 2);

        Assert.Equal(0, eve.Count(nameof(FritzBoxDevice)));
    }

    [Fact]
    public async Task A_user_is_greeted_with_everything()
    {
        await using var bob = await Connect(member);

        await bob.WaitFor(nameof(Gen24System), 1);
        await bob.WaitFor(nameof(FritzBoxDevice), 1);
    }

    [Fact]
    public async Task A_pushed_outlet_reaches_the_user_and_not_the_guest()
    {
        await using var bob = await Connect(member);
        await using var eve = await Connect(guest);
        await bob.WaitFor(nameof(FritzBoxDevice), 1);
        await eve.WaitFor(nameof(Gen24System), 1);

        controlService.AddOrUpdate(OutletId, new ManagedDevice(new FritzBoxDevice { Ain = "11657 0240192" }, null, typeof(IFritzBoxService)));
        await bob.WaitFor(nameof(FritzBoxDevice), 2);

        controlService.AddOrUpdate(InverterId, new ManagedDevice(new Gen24System(), null, typeof(IGen24Service)));
        await bob.WaitFor(nameof(Gen24System), 2);
        await eve.WaitFor(nameof(Gen24System), 2);

        Assert.Equal(0, eve.Count(nameof(FritzBoxDevice)));
    }

    private async Task<Listener> Connect(User user)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options => options.AccessTokenProvider = () => Task.FromResult<string?>(tickets.Issue(user)))
            .ConfigureLogging(l => l.AddTestOutput(LogLevel.Warning))
            .Build();

        var listener = new Listener(connection);
        await connection.StartAsync(TestContext.Current.CancellationToken);
        return listener;
    }

    /// <summary>Counts the device messages a connection receives, by the method name the server sends them under.</summary>
    private sealed class Listener : IAsyncDisposable
    {
        private readonly HubConnection connection;
        private readonly ConcurrentDictionary<string, int> counts = new();

        public Listener(HubConnection connection)
        {
            this.connection = connection;

            foreach (var method in new[] { nameof(Gen24System), nameof(FritzBoxDevice) })
            {
                var captured = method;
                connection.On<string, JsonElement>(captured, (_, _) => counts.AddOrUpdate(captured, 1, (_, n) => n + 1));
            }
        }

        public int Count(string method) => counts.GetValueOrDefault(method);

        public async Task WaitFor(string method, int count)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);

            while (Count(method) < count)
            {
                Assert.True(DateTime.UtcNow < deadline, $"Only {Count(method)} of {count} {method} messages arrived");
                await Task.Delay(20, TestContext.Current.CancellationToken);
            }
        }

        public async ValueTask DisposeAsync() => await connection.DisposeAsync();
    }
}
