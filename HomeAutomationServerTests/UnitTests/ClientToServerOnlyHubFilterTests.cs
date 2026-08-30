using System.Reflection;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The routing rules of <see cref="ClientToServerOnlyHubFilter"/>, checked on the filter itself without a host:
/// server to client is unrestricted, client to server is allowed, client to client is discarded.
/// </summary>
public class ClientToServerOnlyHubFilterTests
{
    private const string CallerConnectionId = "the-caller";
    private const string OtherConnectionId = "somebody-else";

    /// <summary>Every way a hub method can address somebody other than its own caller.</summary>
    public static TheoryData<string> ForeignTargets => new()
    {
        "All", "Others", "AllExcept", "Clients", "Group", "Groups", "GroupExcept", "OthersInGroup", "User", "Users", "AnotherConnection",
    };

    [Theory]
    [MemberData(nameof(ForeignTargets))]
    public async Task A_client_invoked_method_cannot_send_to_anybody_but_its_caller(string target)
    {
        var probe = new HubProbe();

        await probe.InvokeThroughFilter(hub => Resolve(hub.Clients, target).SendAsync("Anything", TestContext.Current.CancellationToken));

        Assert.Empty(probe.Clients.Sent);
    }

    [Fact]
    public async Task The_caller_can_still_be_answered()
    {
        var probe = new HubProbe();

        await probe.InvokeThroughFilter(hub =>
        {
            Assert.Same(probe.Clients.Caller, hub.Clients.Caller);
            return hub.Clients.Caller.SendAsync("Answer", TestContext.Current.CancellationToken);
        });

        Assert.Equal("Caller:Answer", Assert.Single(probe.Clients.Sent));
    }

    [Fact]
    public async Task Addressing_the_callers_own_connection_id_is_the_same_as_addressing_the_caller()
    {
        var probe = new HubProbe();

        await probe.InvokeThroughFilter(hub =>
        {
            Assert.Same(probe.Clients.Caller, hub.Clients.Client(CallerConnectionId));
            return hub.Clients.Client(CallerConnectionId).SendAsync("Answer", TestContext.Current.CancellationToken);
        });

        Assert.Equal("Caller:Answer", Assert.Single(probe.Clients.Sent));
    }

    [Fact]
    public async Task Invoking_another_client_throws_because_nobody_can_answer()
    {
        var probe = new HubProbe();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => probe.InvokeThroughFilter(hub => hub.Clients.Client(OtherConnectionId).InvokeAsync<string>("Question", TestContext.Current.CancellationToken))
        );

        Assert.Contains("must not send messages to other clients", exception.Message);
        Assert.Empty(probe.Clients.Sent);
    }

    [Fact]
    public async Task The_unrestricted_clients_are_put_back_when_the_hub_method_returns()
    {
        var probe = new HubProbe();

        Assert.Same(probe.Clients, probe.Hub.Clients);

        await probe.InvokeThroughFilter(hub =>
        {
            Assert.NotSame(probe.Clients, hub.Clients);
            return Task.CompletedTask;
        });

        Assert.Same(probe.Clients, probe.Hub.Clients);
    }

    [Fact]
    public async Task The_unrestricted_clients_are_put_back_even_when_the_hub_method_throws()
    {
        var probe = new HubProbe();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => probe.InvokeThroughFilter(_ => throw new NotSupportedException("The hub method blew up"))
        );

        Assert.Same(probe.Clients, probe.Hub.Clients);
    }

    [Fact]
    public async Task Hub_lifetime_methods_keep_the_unrestricted_clients()
    {
        // OnConnectedAsync is the server talking, not a client, so it must still be able to replay the device list.
        var probe = new HubProbe();

        await ((IHubFilter)probe.Filter).OnConnectedAsync(probe.LifetimeContext, _ =>
        {
            Assert.Same(probe.Clients, probe.Hub.Clients);
            return probe.Hub.Clients.All.SendAsync("DeviceList", TestContext.Current.CancellationToken);
        });

        Assert.Equal("All:DeviceList", Assert.Single(probe.Clients.Sent));
    }

    private static IClientProxy Resolve(IHubCallerClients clients, string target) => target switch
    {
        "All" => clients.All,
        "Others" => clients.Others,
        "AllExcept" => clients.AllExcept([OtherConnectionId]),
        "Clients" => clients.Clients([OtherConnectionId]),
        "Group" => clients.Group("a-group"),
        "Groups" => clients.Groups(["a-group"]),
        "GroupExcept" => clients.GroupExcept("a-group", [OtherConnectionId]),
        "OthersInGroup" => clients.OthersInGroup("a-group"),
        "User" => clients.User("a-user"),
        "Users" => clients.Users(["a-user"]),
        "AnotherConnection" => clients.Client(OtherConnectionId),
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown target"),
    };

    /// <summary>A hub, its unrestricted clients and the contexts the filter needs, wired together.</summary>
    private sealed class HubProbe
    {
        private static readonly MethodInfo AnyHubMethod = typeof(SomeHub).GetMethod(nameof(SomeHub.SomeMethod))!;
        private static readonly ILoggerFactory Loggers = LoggerFactory.Create(b => b.AddTestOutput());

        private readonly IServiceProvider services = new ServiceCollection().BuildServiceProvider();
        private readonly HubCallerContext callerContext = new FakeHubCallerContext(CallerConnectionId);

        public RecordingHubCallerClients Clients { get; } = new();

        public SomeHub Hub { get; }

        public ClientToServerOnlyHubFilter Filter { get; } = new(Loggers.CreateLogger<ClientToServerOnlyHubFilter>());

        public HubLifetimeContext LifetimeContext => new(callerContext, services, Hub);

        public HubProbe() => Hub = new SomeHub { Clients = Clients, Context = callerContext };

        /// <summary>Runs <paramref name="hubMethodBody"/> the way SignalR runs a method a client invoked.</summary>
        public async Task InvokeThroughFilter(Func<SomeHub, Task> hubMethodBody)
        {
            var context = new HubInvocationContext(callerContext, services, Hub, AnyHubMethod, []);

            await Filter.InvokeMethodAsync(context, async _ =>
            {
                await hubMethodBody(Hub).ConfigureAwait(false);
                return null;
            }).ConfigureAwait(false);
        }
    }

    private sealed class SomeHub : Hub
    {
        public Task SomeMethod() => Task.CompletedTask;
    }
}
