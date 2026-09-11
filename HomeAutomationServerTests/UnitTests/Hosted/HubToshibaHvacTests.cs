using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.Fronius.Services;
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
/// Sending a command to a Toshiba air conditioner over the hub, against the real <see cref="HomeAutomationHub"/> on
/// a real connection: who may, what reaches the service, and how the silent targets come back.
/// </summary>
/// <remarks>
/// The role is the point again, and it is a different one from the Wattpilot's: switching an air conditioner is a
/// <see cref="Roles.PowerUser"/> thing, and <see cref="Roles"/> are flags, so an operator who lacks that bit is
/// refused although they may reconfigure the inverter.
/// </remarks>
public sealed class HubToshibaHvacTests : IAsyncLifetime
{
    private static readonly Guid livingRoomUniqueId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid bedroomUniqueId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");

    private readonly User watcher = TestUsers.Create("bob", Roles.User);
    private readonly User operatorUser = TestUsers.Create("alice", Roles.User | Roles.Operator);
    private readonly User powerUser = TestUsers.Create("carol", Roles.User | Roles.PowerUser);
    private readonly FakeToshibaHvacService toshiba = new() { IsRunning = true, IsConnected = true };
    private readonly ToshibaHvacMappingDevice livingRoom = new() { Name = "Living room", DeviceUniqueId = livingRoomUniqueId };
    private readonly ToshibaHvacMappingDevice bedroom = new() { Name = "Bedroom", DeviceUniqueId = bedroomUniqueId };

    private WebApplication app = null!;
    private Uri hubUri = null!;
    private HubTicketService tickets = null!;

    private string LivingRoomId => ((IHaveUniqueId)livingRoom).Id;
    private string BedroomId => ((IHaveUniqueId)bedroom).Id;

    private sealed class NoWattPilots : IWattPilotServices
    {
        public IWattPilotService? Find(string deviceId) => null;
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Information);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton<IAesKeyProvider>(new TestAesKeyProvider());
        builder.Services.Configure<UserList>(list => list.Users = [watcher, operatorUser, powerUser]);
        builder.Services.AddAuthorization();
        builder.Services.AddHubTicketAuthentication();
        builder.Services.AddHomeAutomationSignalR();

        // What the real hub needs: the devices as the collector publishes them, and the service to send through.
        var controlService = new DataControlService(NullLogger<DataControlService>.Instance);
        controlService.AddOrUpdate(new ManagedDevice(livingRoom, null, typeof(IToshibaHvacService)));
        controlService.AddOrUpdate(new ManagedDevice(bedroom, null, typeof(IToshibaHvacService)));
        builder.Services.AddSingleton<IDataControlService>(controlService);
        builder.Services.AddSingleton<IWattPilotServices>(new NoWattPilots());
        builder.Services.AddSingleton<IToshibaHvacService>(toshiba);

        app = builder.Build();
        app.MapHub<HomeAutomationHub>("/hub").RequireAuthorization(policy => policy.RequireHubTicket());
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        hubUri = new Uri(new Uri(address), "hub");
        tickets = app.Services.GetRequiredService<HubTicketService>();
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public async Task A_power_user_sends_a_command_and_the_service_gets_the_state_and_the_unique_id()
    {
        await using var connection = await Connect(powerUser);
        var state = new ToshibaHvacStateData { IsTurnedOn = true, TargetTemperatureCelsius = 22 };

        var result = await connection.InvokeAsync<ToshibaHvacCommandResult>(nameof(HomeAutomationHub.SendToshibaHvacCommand), new[] { LivingRoomId }, state, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("MB_TEST-00000001", result.MessageId);
        Assert.NotNull(toshiba.SentState);

        // The state travels as the hex string of the wire format and arrives byte for byte; the target is the air
        // conditioner's own unique id, not the id the client knows the device by.
        Assert.Equal(state.ToString(), toshiba.SentState.ToString());
        Assert.Equal([livingRoomUniqueId.ToString("D")], toshiba.SentTargets);
        Assert.Equal(HomeAutomationHub.ToshibaHvacEchoTimeout, toshiba.SentTimeout);
    }

    [Fact]
    public async Task Targets_that_did_not_echo_come_back_under_the_ids_the_caller_used()
    {
        toshiba.SilentTargets.Add(bedroomUniqueId.ToString("D"));
        await using var connection = await Connect(powerUser);

        var result = await connection.InvokeAsync<ToshibaHvacCommandResult>(nameof(HomeAutomationHub.SendToshibaHvacCommand), new[] { LivingRoomId, BedroomId }, new ToshibaHvacStateData { IsTurnedOn = false }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal([BedroomId], result.Unconfirmed);
    }

    [Fact]
    public async Task An_operator_without_the_power_user_role_is_refused_and_so_is_a_watcher()
    {
        await using var operatorConnection = await Connect(operatorUser);
        await using var watcherConnection = await Connect(watcher);

        await Assert.ThrowsAsync<HubException>(() => operatorConnection.InvokeAsync<ToshibaHvacCommandResult>(nameof(HomeAutomationHub.SendToshibaHvacCommand), new[] { LivingRoomId }, new ToshibaHvacStateData(), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<HubException>(() => watcherConnection.InvokeAsync<ToshibaHvacCommandResult>(nameof(HomeAutomationHub.SendToshibaHvacCommand), new[] { LivingRoomId }, new ToshibaHvacStateData(), TestContext.Current.CancellationToken));

        Assert.Null(toshiba.SentState);
    }

    [Fact]
    public async Task A_device_the_server_does_not_have_is_refused_by_name()
    {
        await using var connection = await Connect(powerUser);

        var exception = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<ToshibaHvacCommandResult>(nameof(HomeAutomationHub.SendToshibaHvacCommand), new[] { "somebody-else" }, new ToshibaHvacStateData(), TestContext.Current.CancellationToken));

        Assert.Contains(string.Format(Fronius.Localization.Resources.DeviceNotFound, "somebody-else"), exception.Message);
        Assert.Null(toshiba.SentState);
    }

    [Fact]
    public async Task Without_a_running_service_the_caller_is_told_there_is_no_connection()
    {
        toshiba.IsRunning = false;
        await using var connection = await Connect(powerUser);

        var exception = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<ToshibaHvacCommandResult>(nameof(HomeAutomationHub.SendToshibaHvacCommand), new[] { LivingRoomId }, new ToshibaHvacStateData(), TestContext.Current.CancellationToken));

        Assert.Contains(Fronius.Localization.Resources.NoToshibaHvacConnection, exception.Message);
    }

    private async Task<HubConnection> Connect(User user)
    {
        var ticket = tickets.Issue(user);

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options => options.AccessTokenProvider = () => Task.FromResult<string?>(ticket))
            .AddJsonProtocol(o =>
            {
                // The serialization the client uses, so the state travels here the way it travels there.
                o.PayloadSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                o.PayloadSerializerOptions.IgnoreReadOnlyProperties = true;
                o.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
            })
            .ConfigureLogging(l => l.AddTestOutput(LogLevel.Warning))
            .Build();

        await connection.StartAsync(TestContext.Current.CancellationToken);
        return connection;
    }
}
