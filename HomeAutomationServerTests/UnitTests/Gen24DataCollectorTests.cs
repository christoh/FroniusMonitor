using System.Collections;
using System.Reflection;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Services.DataCollectors;
using De.Hochstaetter.HomeAutomationServerTests.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// That the collector keeps one entry per inverter for as long as it runs, however many rounds it does.
/// </summary>
/// <remarks>
/// Its two dictionaries of running tasks used to be keyed by <c>WebConnection</c>, which does not compare by
/// value, while the sensor loop puts a fresh clone of the connection in as the key on every round. So a round
/// never replaced the entry of the one before it: each left behind another entry holding a task that had already
/// finished, for as long as the server ran, and <c>StopAsync</c> then waited on every one of them.
///
/// Running the collector is the only way to see the growth at all - there is no round without one - so it is
/// started here for real, against a base address that cannot be turned into a request. Every read then fails at
/// once, offline and without touching a socket, and the loops do nothing but cycle, which is all this needs.
/// A closed port is no good for that: on this machine connecting to one takes the full 30 second timeout of the
/// client and then a retry, so the first round never finishes.
/// </remarks>
public class Gen24DataCollectorTests
{
    /// <summary>Not a URL an <c>HttpClient</c> can be built for, so the very first request throws.</summary>
    private const string Nowhere = "http://";

    private static readonly TimeSpan tick = TimeSpan.FromMilliseconds(50);

    private sealed class FixedOptions<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    /// <summary>Takes whatever the collector reports and remembers nothing: none of these reads succeeds anyway.</summary>
    private sealed class SilentControlService : IDataControlService
    {
        // Nothing subscribes and nothing raises it; it is here because the interface asks for it.
        public event EventHandler<DeviceUpdateEventArgs>? DeviceUpdate
        {
            add { }
            remove { }
        }
        public IReadOnlyDictionary<string, ManagedDevice> Entities { get; } = new Dictionary<string, ManagedDevice>();
        public void AddOrUpdate(string id, ManagedDevice entity) { }
        public ValueTask AddOrUpdateAsync(IEnumerable<ManagedDevice> entities, CancellationToken token = default) => ValueTask.CompletedTask;
        public void AddOrUpdate(ManagedDevice entity) { }
        public ValueTask RemoveAsync(IEnumerable<string> ids, CancellationToken token = default) => ValueTask.CompletedTask;
        public void Remove(string id) { }
    }

    private static Gen24DataCollector NewCollector() => new
    (
        new Logger<Gen24DataCollector>(new LoggerFactory([new TestOutputLoggerProvider(LogLevel.Warning)])),
        new FixedOptions<Gen24DataCollectorParameters>(new Gen24DataCollectorParameters
        {
            Connections = [new WebConnection { BaseUrl = Nowhere, UserName = "customer", Password = "secret" }],
            RefreshRate = tick,
            ConfigRefreshRate = tick,
            LogDirectory = Path.GetTempPath(),
        }),
        new SilentControlService()
    );

    private static int CountOf(Gen24DataCollector collector, string fieldName) =>
        ((ICollection)typeof(Gen24DataCollector)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(collector)!).Count;

    [Fact]
    public async Task However_many_rounds_it_does_there_is_one_entry_per_inverter()
    {
        var collector = NewCollector();
        await collector.StartAsync(TestContext.Current.CancellationToken);

        // Long enough for a dozen rounds of both loops. Keyed by the connection, each of them left an entry
        // behind: this ran up to sensors=13 before the key became the inverter.
        await Task.Delay(tick * 12, TestContext.Current.CancellationToken);

        try
        {
            Assert.Equal(1, CountOf(collector, "runningSensorTasks"));
            Assert.Equal(1, CountOf(collector, "runningConfigTasks"));
            Assert.Equal(1, CountOf(collector, "configReadRequests"));
        }
        finally
        {
            await collector.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Stopping_leaves_nothing_of_the_run_behind()
    {
        // StartAsync builds new inverters, so anything kept from the run before would never be replaced either.
        var collector = NewCollector();
        await collector.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(tick * 2, TestContext.Current.CancellationToken);

        await collector.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, CountOf(collector, "runningSensorTasks"));
        Assert.Equal(0, CountOf(collector, "runningConfigTasks"));
        Assert.Equal(0, CountOf(collector, "configReadRequests"));
    }
}
