using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// A real SignalR client that remembers everything it was sent, and can wait for one particular message.
/// </summary>
internal sealed class ProbeClient : IAsyncDisposable
{
    private readonly ConcurrentQueue<string> received = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> waiters = new();

    public HubConnection Connection { get; }

    /// <summary>The connection id, as the server told us in its greeting.</summary>
    public string ConnectionId { get; private set; } = string.Empty;

    public IReadOnlyList<string> Received => [.. received];

    private ProbeClient(Uri hubUri)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .ConfigureLogging(l => l.AddTestOutput(LogLevel.Warning))
            .Build();

        foreach (var method in new[] { DirectionProbeHub.Welcome, DirectionProbeHub.Broadcast, DirectionProbeHub.Direct, DirectionProbeHub.ServerPush })
        {
            var captured = method;
            Connection.On<string>(captured, payload => Record(captured, payload));
        }

        // A client that answers, so a test can tell "the invocation never arrived" from "the client had no handler".
        Connection.On(DirectionProbeHub.Question, () =>
        {
            Record(DirectionProbeHub.Question, "asked");
            return "the answer";
        });
    }

    public static async Task<ProbeClient> ConnectAsync(Uri hubUri, CancellationToken token)
    {
        var client = new ProbeClient(hubUri);
        await client.Connection.StartAsync(token).ConfigureAwait(false);
        client.ConnectionId = await client.WaitFor(DirectionProbeHub.Welcome).ConfigureAwait(false);
        return client;
    }

    /// <summary>Waits for one message of <paramref name="method"/>, or gives up.</summary>
    public Task<string> WaitFor(string method, int timeoutSeconds = 15) => Waiter(method).Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));

    public bool Got(string method) => received.Any(entry => entry.StartsWith($"{method}:", StringComparison.Ordinal));

    public async ValueTask DisposeAsync() => await Connection.DisposeAsync().ConfigureAwait(false);

    private void Record(string method, string payload)
    {
        received.Enqueue($"{method}:{payload}");
        Waiter(method).TrySetResult(payload);
    }

    private TaskCompletionSource<string> Waiter(string method) =>
        waiters.GetOrAdd(method, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
}
