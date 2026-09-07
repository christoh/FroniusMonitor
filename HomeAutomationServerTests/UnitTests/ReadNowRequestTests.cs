using System.Diagnostics;
using De.Hochstaetter.Fronius.Services.DataCollectors;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The wait a data collector does between two reads, which whoever writes to a device can cut short.
/// </summary>
/// <remarks>
/// It is a semaphore with one place in it, and the interesting behaviour is at the edges: asked for before the
/// wait even starts, asked for twice, cancelled. Those are cheap to pin down here and impossible to see in the
/// collector itself, which needs an inverter on the network to do anything at all.
/// </remarks>
public class ReadNowRequestTests
{
    private static readonly TimeSpan tooLongToWaitFor = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Without_a_request_the_wait_lasts_the_interval()
    {
        using var request = new ReadNowRequest();
        var stopwatch = Stopwatch.StartNew();

        var wasAskedFor = await request.WaitAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);

        Assert.False(wasAskedFor);
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(150), $"only waited {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task A_request_that_came_first_is_not_lost()
    {
        // The order matters: a setting is written while the collector is asleep, and the request has to survive
        // until it wakes up and asks.
        using var request = new ReadNowRequest();
        request.Request();

        Assert.True(await request.WaitAsync(tooLongToWaitFor, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_request_ends_a_wait_that_is_already_running()
    {
        using var request = new ReadNowRequest();
        var waiting = request.WaitAsync(tooLongToWaitFor, TestContext.Current.CancellationToken);

        request.Request();

        Assert.True(await waiting);
    }

    [Fact]
    public async Task Asking_twice_is_the_same_as_asking_once()
    {
        // One read answers both: it happens after the second request and therefore sees what it wrote. A second
        // read would only be a request the collector cannot refuse.
        using var request = new ReadNowRequest();
        request.Request();
        request.Request();

        Assert.True(await request.WaitAsync(tooLongToWaitFor, TestContext.Current.CancellationToken));
        Assert.False(await request.WaitAsync(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_request_made_while_the_read_is_running_is_answered_by_the_next_one()
    {
        // The write may have reached the inverter after the read started, so the request must not be swallowed.
        using var request = new ReadNowRequest();
        await request.WaitAsync(TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken);
        request.Request();

        Assert.True(await request.WaitAsync(tooLongToWaitFor, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Cancelling_the_collector_ends_the_wait()
    {
        using var request = new ReadNowRequest();
        using var tokenSource = new CancellationTokenSource();
        var waiting = request.WaitAsync(tooLongToWaitFor, tokenSource.Token);

        await tokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
    }
}
