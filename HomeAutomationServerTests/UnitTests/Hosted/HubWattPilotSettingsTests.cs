using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.HomeAutomationServer.Hubs;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// Writing Wattpilot settings over the hub, against the real <see cref="HomeAutomationHub"/> on a real
/// connection: who may, what reaches the service that owns the charger, and what the caller gets back.
/// </summary>
/// <remarks>
/// The role is the point. The ticket that opens the connection only proves <see cref="Roles.User"/>, and every
/// write to a device asks for <see cref="Roles.Operator"/> - so a user who can watch the charger must not be able
/// to change it, and the hub method has to say so itself.
/// </remarks>
public sealed class HubWattPilotSettingsTests : IAsyncLifetime
{
    private const string ChargerId = "fronius;wattpilot;424242";

    private readonly User watcher = TestUsers.Create("bob", Roles.User);
    private readonly User operatorUser = TestUsers.Create("alice", Roles.User | Roles.Operator);
    private readonly FakeWattPilotService charger = new();

    private WebApplication app = null!;
    private Uri hubUri = null!;
    private HubTicketService tickets = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Information);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IAesKeyProvider>(new TestAesKeyProvider());
        builder.Services.Configure<UserList>(list => list.Users = [watcher, operatorUser]);
        builder.Services.AddAuthorization();
        builder.Services.AddHubTicketAuthentication();
        builder.Services.AddHomeAutomationSignalR();

        // What the real hub needs: an empty device list to greet a connection with, and the way to the charger.
        builder.Services.AddSingleton<IDataControlService, DataControlService>();
        builder.Services.AddSingleton<IWattPilotServices>(new FakeWattPilotServices(ChargerId, charger));

        app = builder.Build();
        app.MapHub<HomeAutomationHub>("/hub").RequireAuthorization(policy => policy.RequireHubTicket());
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        hubUri = new Uri(new Uri(address), "hub");
        tickets = app.Services.GetRequiredService<HubTicketService>();
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task An_operator_writes_what_differs_and_gets_the_outcome()
    {
        await using var connection = await Connect(operatorUser);
        var loaded = new WattPilot { DeviceName = "Garage", MaximumChargingCurrent = 16 };
        var wanted = new WattPilot { DeviceName = "Garage", MaximumChargingCurrent = 10 };

        var result = await connection.InvokeAsync<WattPilotWriteResult>(nameof(HomeAutomationHub.SetWattPilotSettings), ChargerId, wanted, loaded, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(charger.BeginSendValuesCalled);
        Assert.True(charger.WaitSendValuesCalled);
        Assert.NotNull(charger.SentWanted);
        Assert.NotNull(charger.SentOld);

        // The hub hands both to the service and lets it take the difference: the service, not the client, decides
        // which keys go to the charger - and the two arrive as what the client sent.
        Assert.Equal<byte?>(10, charger.SentWanted.MaximumChargingCurrent);
        Assert.Equal<byte?>(16, charger.SentOld.MaximumChargingCurrent);
        Assert.Equal("Garage", charger.SentWanted.DeviceName);
    }

    [Fact]
    public async Task What_the_charger_did_not_confirm_comes_back_to_the_caller()
    {
        charger.ConfirmsNothing = true;
        await using var connection = await Connect(operatorUser);

        var result = await connection.InvokeAsync<WattPilotWriteResult>(nameof(HomeAutomationHub.SetWattPilotSettings), ChargerId, new WattPilot { MaximumChargingCurrent = 10 }, new WattPilot { MaximumChargingCurrent = 16 }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal([FakeWattPilotService.UnconfirmedWrite], result.Unconfirmed);
    }

    [Fact]
    public async Task A_user_who_may_only_watch_is_refused()
    {
        await using var connection = await Connect(watcher);

        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<WattPilotWriteResult>(nameof(HomeAutomationHub.SetWattPilotSettings), ChargerId, new WattPilot(), new WattPilot(), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync(nameof(HomeAutomationHub.RebootWattPilot), ChargerId, TestContext.Current.CancellationToken));

        Assert.Null(charger.SentWanted);
        Assert.False(charger.RebootCalled);
    }

    [Fact]
    public async Task A_charger_the_server_does_not_have_is_refused_by_name()
    {
        await using var connection = await Connect(operatorUser);

        var exception = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<WattPilotWriteResult>(nameof(HomeAutomationHub.SetWattPilotSettings), "somebody-else", new WattPilot(), new WattPilot(), TestContext.Current.CancellationToken));

        Assert.Contains(Fronius.Localization.Resources.NoWattPilotConnection, exception.Message);
    }

    [Fact]
    public async Task An_operator_may_reboot()
    {
        await using var connection = await Connect(operatorUser);

        await connection.InvokeAsync(nameof(HomeAutomationHub.RebootWattPilot), ChargerId, TestContext.Current.CancellationToken);

        Assert.True(charger.RebootCalled);
    }

    private async Task<HubConnection> Connect(User user)
    {
        var ticket = tickets.Issue(user);

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options => options.AccessTokenProvider = () => Task.FromResult<string?>(ticket))
            .AddJsonProtocol(o =>
            {
                // The serialization the client uses, so a WattPilot travels here the way it travels there.
                o.PayloadSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                o.PayloadSerializerOptions.IgnoreReadOnlyProperties = true;
                o.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
            })
            .ConfigureLogging(l => l.AddTestOutput(LogLevel.Warning))
            .Build();

        await connection.StartAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    private sealed class FakeWattPilotServices(string id, IWattPilotService service) : IWattPilotServices
    {
        public IWattPilotService? Find(string deviceId) => deviceId == id ? service : null;
    }

    /// <summary>A charger's service that remembers what it was asked and answers as told.</summary>
    private sealed class FakeWattPilotService : IWattPilotService
    {
        public const string UnconfirmedWrite = "MaximumChargingCurrent = '10'";

        public bool ConfirmsNothing { get; set; }
        public bool BeginSendValuesCalled { get; private set; }
        public bool WaitSendValuesCalled { get; private set; }
        public bool RebootCalled { get; private set; }
        public WattPilot? SentWanted { get; private set; }
        public WattPilot? SentOld { get; private set; }

        public event EventHandler<NewWattPilotFirmwareEventArgs>? NewFirmwareAvailable;
        public event EventHandler<WattPilotServiceStoppedEventArgs>? OnLostConnection;
        public event EventHandler<WattPilotUpdateEventArgs>? OnUpdate;

        public WebConnection? Connection => null;
        public WattPilot? WattPilot => null;

        public IReadOnlyList<WattPilotAcknowledge> UnsuccessfulWrites => ConfirmsNothing
            ? [new WattPilotAcknowledge(1, typeof(WattPilot).GetProperty(nameof(Fronius.Models.Charging.WattPilot.MaximumChargingCurrent))!, (byte)10)]
            : [];

        public ValueTask StartAsync(WebConnection connection) => throw new NotSupportedException();
        public ValueTask StopAsync() => throw new NotSupportedException();
        public ValueTask SendValue(WattPilot instance, string propertyName) => throw new NotSupportedException();
        public void OpenChargingLog() => throw new NotSupportedException();
        public void OpenConfigPdf() => throw new NotSupportedException();

        public void BeginSendValues() => BeginSendValuesCalled = true;

        public Task WaitSendValues(int timeout = 5000)
        {
            WaitSendValuesCalled = true;
            return ConfirmsNothing ? throw new TimeoutException() : Task.CompletedTask;
        }

        public ValueTask<List<string>> Send(WattPilot? localWattPilot = null, WattPilot? oldWattPilot = null)
        {
            SentWanted = localWattPilot;
            SentOld = oldWattPilot;
            return ValueTask.FromResult(new List<string>());
        }

        public Task RebootWattPilot()
        {
            RebootCalled = true;
            return Task.CompletedTask;
        }

        // The events are part of the contract and nothing here raises them; naming them keeps the compiler quiet
        // without pretending they are used.
        private void Unused() => _ = (NewFirmwareAvailable, OnLostConnection, OnUpdate);
    }
}
