using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace De.Hochstaetter.HomeAutomationClient.Services;

/// <summary>
/// Keeps a connection open only while somebody can see what it delivers. The hub connection is the one this was
/// written for: while every window that shows updates is minimized or hidden, the server would push a message a
/// second into a client nobody looks at, so the connection is dropped instead - after a grace period, because a
/// window minimized and restored a moment later should not cost a reconnect - and opened again the moment a window
/// comes back, followed by a catch-up for whatever was missed in between.
/// </summary>
/// <remarks>
/// <para>
/// Nothing in here knows what a hub is. The three things it does are handed in as delegates, so it can be tested
/// with counters, and the delays are parameters for the same reason. <see cref="IVisibilityService"/> says when
/// somebody looks; how it knows is not this class's business.
/// </para>
/// <para>
/// All decisions run on one worker loop, woken by <see cref="IVisibilityService.IsAnyVisibleChanged"/>. That is
/// what serializes a connect against a disconnect: a visibility that flips back and forth while a connect is in
/// flight is looked at again once the connect is done, never acted on in parallel. The wake-up is a channel that
/// holds one item, so however often the visibility changes while the worker is busy, it wakes up once and reads
/// the current state.
/// </para>
/// </remarks>
internal sealed class ConnectionGate : IAsyncDisposable
{
    /// <summary>How long every window has to stay out of sight before the connection is dropped.</summary>
    public static readonly TimeSpan DefaultDisconnectDelay = TimeSpan.FromSeconds(5);

    /// <summary>How long to wait before trying again after a connect failed while a window is in sight.</summary>
    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(10);

    private readonly IVisibilityService visibility;
    private readonly Func<CancellationToken, Task> connect;
    private readonly Func<Task> disconnect;
    private readonly Func<Task> catchUp;
    private readonly TimeSpan disconnectDelay;
    private readonly TimeSpan retryDelay;
    private readonly ILogger logger;
    private readonly Channel<bool> wakeUps = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private CancellationTokenSource? stopping;
    private Task? worker;

    /// <param name="visibility">Who says whether anybody looks.</param>
    /// <param name="connect">Opens the connection. Its exception is the caller's on the first connect and logged, then retried, on every later one.</param>
    /// <param name="disconnect">Closes the connection. The connection object stays, so <paramref name="connect"/> can open it again.</param>
    /// <param name="catchUp">Runs after every connect but the first: whatever the connection would have delivered in the meantime has to be fetched.</param>
    /// <param name="disconnectDelay">The grace period; <see cref="DefaultDisconnectDelay"/> where <see langword="null"/>.</param>
    /// <param name="retryDelay">The pause between two attempts of a failed connect; <see cref="DefaultRetryDelay"/> where <see langword="null"/>.</param>
    public ConnectionGate(IVisibilityService visibility, Func<CancellationToken, Task> connect, Func<Task> disconnect, Func<Task> catchUp, ILogger logger, TimeSpan? disconnectDelay = null, TimeSpan? retryDelay = null)
    {
        this.visibility = visibility;
        this.connect = connect;
        this.disconnect = disconnect;
        this.catchUp = catchUp;
        this.logger = logger;
        this.disconnectDelay = disconnectDelay ?? DefaultDisconnectDelay;
        this.retryDelay = retryDelay ?? DefaultRetryDelay;
    }

    /// <summary>Whether the connection is open as far as this gate knows.</summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Connects now if anybody looks - and lets the exception through if that fails, so the caller can report it
    /// the way it reports every other start-up failure - then starts following the visibility.
    /// </summary>
    public async Task StartAsync(CancellationToken token = default)
    {
        if (worker != null)
        {
            throw new InvalidOperationException("The gate is already running.");
        }

        if (visibility.IsAnyVisible)
        {
            await connect(token).ConfigureAwait(false);
            IsConnected = true;
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Nothing is visible; the connection is opened once something is.");
        }

        stopping = new CancellationTokenSource();
        visibility.IsAnyVisibleChanged += OnVisibilityChanged;
        worker = Task.Run(() => RunAsync(stopping.Token));
    }

    /// <summary>
    /// Stops following the visibility. The connection is left as it is: whoever owns it disposes it, and a
    /// disconnect here would race that.
    /// </summary>
    public async Task StopAsync()
    {
        visibility.IsAnyVisibleChanged -= OnVisibilityChanged;

        if (stopping is { } cancel)
        {
            await cancel.CancelAsync().ConfigureAwait(false);
        }

        if (worker is { } running)
        {
            await running.ConfigureAwait(false);
        }

        stopping?.Dispose();
        stopping = null;
        worker = null;
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    /// <summary>
    /// The connection closed on its own - lost for good, after whatever reconnecting it does itself has given up.
    /// The gate opens it again as long as somebody looks, with its own retries.
    /// </summary>
    public void NotifyDisconnected()
    {
        IsConnected = false;
        wakeUps.Writer.TryWrite(true);
    }

    private void OnVisibilityChanged(object? sender, EventArgs e) => wakeUps.Writer.TryWrite(true);

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await wakeUps.Reader.ReadAsync(token).ConfigureAwait(false);
                await SettleAsync(token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Stopping, which is the one way out of the loop.
        }
    }

    /// <summary>
    /// Brings the connection in line with the visibility: opens it if somebody looks and it is closed, closes it
    /// if nobody has looked for <see cref="disconnectDelay"/>. Comes back as soon as the two agree, and is
    /// woken again by the next change.
    /// </summary>
    private async Task SettleAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var wanted = visibility.IsAnyVisible;

            if (wanted == IsConnected)
            {
                return;
            }

            if (wanted)
            {
                if (await TryConnectAsync(token).ConfigureAwait(false))
                {
                    continue;
                }

                // Still wanted and still not connected, so wait and try again - unless a change of visibility
                // comes first, which is looked at right away.
                await WaitForWakeUpAsync(retryDelay, token).ConfigureAwait(false);
                continue;
            }

            // A window minimized and restored a moment later must not cost a reconnect and a catch-up, so the drop
            // waits. A wake-up during the wait starts over with a fresh look at the visibility.
            if (await WaitForWakeUpAsync(disconnectDelay, token).ConfigureAwait(false))
            {
                continue;
            }

            if (visibility.IsAnyVisible)
            {
                continue;
            }

            await DisconnectAsync().ConfigureAwait(false);
        }
    }

    private async Task<bool> TryConnectAsync(CancellationToken token)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Something is visible again; reconnecting.");
            }

            await connect(token).ConfigureAwait(false);
            IsConnected = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reconnecting failed; trying again in {Delay}.", retryDelay);
            return false;
        }

        try
        {
            // Every reconnect has a gap before it, and the connection does not deliver what fell into the gap.
            await catchUp().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The connection is up and delivers from here on; only the gap stays unfilled until the next push.
            logger.LogError(ex, "Catching up after the reconnect failed.");
        }

        return true;
    }

    private async Task DisconnectAsync()
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Nothing has been visible for {Delay}; disconnecting.", disconnectDelay);
            }

            await disconnect().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Disconnecting failed.");
        }
        finally
        {
            // Whatever the disconnect did, the next look at a window opens the connection anew rather than
            // trusting one that may be half closed.
            IsConnected = false;
        }
    }

    /// <summary>Waits for a wake-up or for <paramref name="delay"/>, whichever comes first, and says which it was.</summary>
    /// <remarks>
    /// Through a token and not <c>WaitAsync</c>: a read that <c>WaitAsync</c> gives up on stays pending inside the
    /// channel and would swallow the next wake-up unseen. Cancelling the read itself lets the channel drop it.
    /// </remarks>
    private async Task<bool> WaitForWakeUpAsync(TimeSpan delay, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(delay);

        try
        {
            await wakeUps.Reader.ReadAsync(timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return false;
        }
    }
}
