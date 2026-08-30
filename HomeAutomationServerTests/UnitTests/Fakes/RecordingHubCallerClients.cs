using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>
/// The <see cref="IHubCallerClients"/> a hub would really get. Every proxy it hands out writes
/// "<c>&lt;target&gt;:&lt;method&gt;</c>" into <see cref="Sent"/>, so a test can tell exactly what left the hub -
/// and, more to the point, what did not.
/// </summary>
internal sealed class RecordingHubCallerClients : IHubCallerClients
{
    private readonly List<string> sent = [];

    public IReadOnlyList<string> Sent
    {
        get
        {
            lock (sent)
            {
                return [.. sent];
            }
        }
    }

    // A single instance, so a test can assert that the restricted wrapper hands out this very proxy.
    public ISingleClientProxy Caller { get; }

    public RecordingHubCallerClients() => Caller = Proxy("Caller");

    public ISingleClientProxy Client(string connectionId) => Proxy($"Client({connectionId})");

    public IClientProxy All => Proxy("All");

    public IClientProxy Others => Proxy("Others");

    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy("AllExcept");

    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy("Clients");

    public IClientProxy Group(string groupName) => Proxy($"Group({groupName})");

    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy("Groups");

    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy("GroupExcept");

    public IClientProxy OthersInGroup(string groupName) => Proxy("OthersInGroup");

    public IClientProxy User(string userId) => Proxy("User");

    public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy("Users");

    IClientProxy IHubCallerClients<IClientProxy>.Caller => Caller;

    IClientProxy IHubClients<IClientProxy>.Client(string connectionId) => Client(connectionId);

    private RecordingClientProxy Proxy(string target) => new(target, sent);
}

internal sealed class RecordingClientProxy(string target, List<string> sent) : ISingleClientProxy
{
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Record($"{target}:{method}");
        return Task.CompletedTask;
    }

    public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Record($"{target}:{method}:invoke");
        return Task.FromResult<T>(default!);
    }

    private void Record(string entry)
    {
        lock (sent)
        {
            sent.Add(entry);
        }
    }
}

/// <summary>
/// The bare minimum of a <see cref="HubCallerContext"/>: a connection id to compare against.
/// </summary>
internal sealed class FakeHubCallerContext(string connectionId) : HubCallerContext
{
    public override string ConnectionId => connectionId;

    public override string? UserIdentifier => null;

    public override ClaimsPrincipal? User => null;

    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    public override IFeatureCollection Features { get; } = new FeatureCollection();

    public override CancellationToken ConnectionAborted => CancellationToken.None;

    public override void Abort()
    {
    }
}
