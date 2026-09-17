using De.Hochstaetter.HomeAutomationClient.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The gate that keeps the hub connection open only while somebody can see a window: what it opens, what it
/// closes, when, and what it fetches afterwards. The connection itself is three counters here.
/// </summary>
public sealed class ConnectionGateTests : IAsyncDisposable
{
    /// <summary>Short, so the tests are quick; long enough that a flicker fits inside comfortably.</summary>
    private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(150);

    private static readonly TimeSpan patience = TimeSpan.FromSeconds(5);

    private readonly FakeVisibilityService visibility = new();
    private readonly List<string> log = [];
    private ConnectionGate? gate;
    private Exception? nextConnectFailure;

    private int Connects => log.Count(entry => entry == "connect");

    private int Disconnects => log.Count(entry => entry == "disconnect");

    private int CatchUps => log.Count(entry => entry == "catchUp");

    private async Task<ConnectionGate> StartAsync()
    {
        gate = new ConnectionGate(visibility, Connect, Disconnect, CatchUp, NullLogger.Instance, delay, delay);
        await gate.StartAsync();
        return gate;
    }

    private Task Connect(CancellationToken token)
    {
        if (Interlocked.Exchange(ref nextConnectFailure, null) is { } failure)
        {
            throw failure;
        }

        lock (log)
        {
            log.Add("connect");
        }

        return Task.CompletedTask;
    }

    private Task Disconnect()
    {
        lock (log)
        {
            log.Add("disconnect");
        }

        return Task.CompletedTask;
    }

    private Task CatchUp()
    {
        lock (log)
        {
            log.Add("catchUp");
        }

        return Task.CompletedTask;
    }

    private async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + patience;

        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, $"Timed out waiting; the log is [{string.Join(", ", log)}].");
            await Task.Delay(10);
        }
    }

    /// <summary>Waits long enough for a disconnect that is not supposed to happen to have happened.</summary>
    private static Task LetTheDelayPassAsync() => Task.Delay(delay * 3);

    public async ValueTask DisposeAsync()
    {
        if (gate != null)
        {
            await gate.DisposeAsync();
        }
    }

    [Fact]
    public async Task Starting_while_visible_connects_once_and_fetches_nothing()
    {
        var gate = await StartAsync();

        Assert.True(gate.IsConnected);
        Assert.Equal(["connect"], log);
    }

    [Fact]
    public async Task Starting_while_nothing_is_visible_waits_for_a_window()
    {
        visibility.IsAnyVisible = false;
        var gate = await StartAsync();

        Assert.False(gate.IsConnected);
        Assert.Empty(log);

        visibility.IsAnyVisible = true;
        await WaitUntilAsync(() => gate.IsConnected);

        // Devices were loaded when the service started, and time has passed since, so this connect catches up.
        await WaitUntilAsync(() => CatchUps == 1);
        Assert.Equal(["connect", "catchUp"], log);
    }

    [Fact]
    public async Task The_connection_is_dropped_after_the_last_window_has_been_out_of_sight_for_the_delay()
    {
        var gate = await StartAsync();
        var hidden = DateTime.UtcNow;

        visibility.IsAnyVisible = false;
        await WaitUntilAsync(() => !gate.IsConnected);

        Assert.True(DateTime.UtcNow - hidden >= delay - TimeSpan.FromMilliseconds(20), "Dropped before the grace period was over.");
        Assert.Equal(["connect", "disconnect"], log);
    }

    [Fact]
    public async Task A_window_restored_within_the_delay_keeps_the_connection()
    {
        var gate = await StartAsync();

        visibility.IsAnyVisible = false;
        await Task.Delay(delay / 4);
        visibility.IsAnyVisible = true;
        await LetTheDelayPassAsync();

        Assert.True(gate.IsConnected);
        Assert.Equal(["connect"], log);
    }

    [Fact]
    public async Task A_window_coming_back_reconnects_at_once_and_catches_up()
    {
        var gate = await StartAsync();

        visibility.IsAnyVisible = false;
        await WaitUntilAsync(() => !gate.IsConnected);

        var shown = DateTime.UtcNow;
        visibility.IsAnyVisible = true;
        await WaitUntilAsync(() => CatchUps == 1);

        Assert.True(gate.IsConnected);
        Assert.True(DateTime.UtcNow - shown < delay, "The reconnect waited, and there is nothing to wait for.");
        Assert.Equal(["connect", "disconnect", "connect", "catchUp"], log);
    }

    [Fact]
    public async Task A_failed_reconnect_is_tried_again()
    {
        var gate = await StartAsync();

        visibility.IsAnyVisible = false;
        await WaitUntilAsync(() => !gate.IsConnected);

        nextConnectFailure = new HttpRequestException("The server is away.");
        visibility.IsAnyVisible = true;
        await WaitUntilAsync(() => gate.IsConnected);

        Assert.Equal(2, Connects);
        Assert.Equal(1, CatchUps);
    }

    [Fact]
    public async Task A_connection_lost_on_its_own_is_opened_again_while_somebody_looks()
    {
        var gate = await StartAsync();

        gate.NotifyDisconnected();
        await WaitUntilAsync(() => CatchUps == 1);

        Assert.True(gate.IsConnected);
        Assert.Equal(["connect", "connect", "catchUp"], log);
    }

    [Fact]
    public async Task A_connection_lost_while_nobody_looks_waits_for_a_window()
    {
        var gate = await StartAsync();

        visibility.IsAnyVisible = false;
        gate.NotifyDisconnected();
        await LetTheDelayPassAsync();

        Assert.False(gate.IsConnected);
        Assert.Equal(["connect"], log);

        visibility.IsAnyVisible = true;
        await WaitUntilAsync(() => CatchUps == 1);
        Assert.True(gate.IsConnected);
    }

    [Fact]
    public async Task Stopping_leaves_the_connection_to_its_owner()
    {
        var gate = await StartAsync();
        await gate.StopAsync();

        visibility.IsAnyVisible = false;
        await LetTheDelayPassAsync();

        Assert.Equal(["connect"], log);
    }

    [Fact]
    public async Task The_first_connect_fails_into_the_callers_hands()
    {
        nextConnectFailure = new HttpRequestException("No server.");
        gate = new ConnectionGate(visibility, Connect, Disconnect, CatchUp, NullLogger.Instance, delay, delay);

        await Assert.ThrowsAsync<HttpRequestException>(() => gate.StartAsync());
        Assert.False(gate.IsConnected);
    }
}
