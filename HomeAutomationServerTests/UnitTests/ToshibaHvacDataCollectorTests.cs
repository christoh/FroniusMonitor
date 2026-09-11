using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.Fronius.Services.DataCollectors;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What <see cref="ToshibaHvacDataCollector"/> puts into the control service, and when: the devices of the account
/// once the service is up, the one device again when the IoT Hub reports it, and nothing once it is stopped.
/// </summary>
public sealed class ToshibaHvacDataCollectorTests : IAsyncDisposable
{
    private readonly FakeToshibaHvacService service = new();
    private readonly IDataControlService controlService = new DataControlService(NullLogger<DataControlService>.Instance);
    private readonly List<DeviceUpdateEventArgs> updates = [];
    private readonly ToshibaHvacMappingDevice livingRoom = new() { Name = "Living room", DeviceUniqueId = Guid.Parse("11111111-2222-3333-4444-555555555555") };
    private readonly ToshibaHvacMappingDevice bedroom = new() { Name = "Bedroom", DeviceUniqueId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa") };
    private ToshibaHvacDataCollector? collector;

    public ToshibaHvacDataCollectorTests()
    {
        service.Devices.AddRange([livingRoom, bedroom]);
        controlService.DeviceUpdate += (_, e) => updates.Add(e);
    }

    public async ValueTask DisposeAsync()
    {
        if (collector != null)
        {
            await collector.DisposeAsync();
        }
    }

    [Fact]
    public async Task Starting_publishes_every_device_of_the_account_under_its_id()
    {
        collector = Create("somebody@example.com");

        await collector.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, service.StartCalls);
        Assert.Equal("004711", service.StartedWithDeviceId);
        Assert.Equal("somebody@example.com", service.StartedWith?.UserName);

        var published = controlService.Entities;
        Assert.Equal(2, published.Count);
        Assert.Same(livingRoom, published[((IHaveUniqueId)livingRoom).Id].Device);
        Assert.Same(bedroom, published[((IHaveUniqueId)bedroom).Id].Device);
        Assert.All(published.Values, d => Assert.Equal(typeof(IToshibaHvacService), d.ServiceType));
        Assert.All(published.Values, d => Assert.False(d.SupportsPushMessages));
    }

    [Fact]
    public async Task A_live_update_publishes_that_device_again_and_nothing_else()
    {
        collector = Create("somebody@example.com");
        await collector.StartAsync(TestContext.Current.CancellationToken);
        updates.Clear();

        service.RaiseDeviceUpdated(bedroom);

        var update = Assert.Single(updates);
        Assert.Equal(DeviceAction.Change, update.DeviceAction);
        Assert.Same(bedroom, update.Device.Device);
    }

    [Fact]
    public async Task Stopping_withdraws_the_devices_and_stops_the_service()
    {
        collector = Create("somebody@example.com");
        await collector.StartAsync(TestContext.Current.CancellationToken);

        await collector.StopAsync(TestContext.Current.CancellationToken);

        Assert.Empty(controlService.Entities);
        Assert.False(service.IsRunning);
        Assert.Contains(updates, u => u.DeviceAction == DeviceAction.Delete && ReferenceEquals(u.Device.Device, livingRoom));
    }

    [Fact]
    public async Task Without_an_account_nothing_is_started()
    {
        collector = Create(string.Empty);

        await collector.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, service.StartCalls);
        Assert.Empty(controlService.Entities);
    }

    [Fact]
    public async Task A_service_that_does_not_come_up_publishes_nothing()
    {
        service.StartSucceeds = false;
        collector = Create("somebody@example.com");

        await collector.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, service.StartCalls);
        Assert.Empty(controlService.Entities);
    }

    private ToshibaHvacDataCollector Create(string userName)
    {
        var options = new ServiceCollection()
            .AddOptions()
            .Configure<ToshibaHvacDataCollectorParameters>(p =>
            {
                p.Connection = new AzureConnection { BaseUrl = "https://example.com", UserName = userName, Password = "secret" };
                p.AzureDeviceId = "004711";
                p.MappingRefreshRate = TimeSpan.FromHours(1);
            })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<ToshibaHvacDataCollectorParameters>>();

        return new ToshibaHvacDataCollector(NullLogger<ToshibaHvacDataCollector>.Instance, options, controlService, service);
    }
}
