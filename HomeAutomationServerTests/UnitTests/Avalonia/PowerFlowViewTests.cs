using Avalonia.Controls;
using Avalonia.VisualTree;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using De.Hochstaetter.HomeAutomationClient.Views;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The power flow page in a window of its own: a card per node, a wire to every one of them, and figures that
/// update without the cards being rebuilt. See the memory document <c>PowerFlowPage.Lifecycle</c>.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class PowerFlowViewTests
{
    private const string Title = "Power flow";

    private static async Task<FakeUpdateService> StartAsync()
    {
        var service = new FakeUpdateService();
        var presenter = new WindowPresenter();

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IUpdateService>(service)
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter)
            .AddTransient<PowerFlowView>()
            .AddTransient<PowerFlowViewModel>());

        var mainWindow = new Window { Title = "main", Width = 400, Height = 300 };
        mainWindow.Show();

        // Two inverters with two trackers each, one of them with a battery, and three metered consumers: a heat
        // pump, a fridge, a car.
        service.Inverters.Add(Inverter("inv-a", 3620, 1010, 4200, storage: -1120, soc: 0.68));
        service.Inverters.Add(Inverter("inv-b", 1010, 0, 950));
        service.SitePowerFlow.LoadPower = -5100;
        service.SitePowerFlow.InverterAcPower = 5150;
        service.SitePowerFlow.GridPower = -50;
        service.SitePowerFlow.SolarPower = 5640;
        service.SitePowerFlow.StoragePower = -1120;
        service.AllPowerConsumers.Add(Plug("hp", "Heat pump", 620));
        service.AllPowerConsumers.Add(Plug("fridge", "Fridge", 0));
        service.AllPowerConsumers.Add(new KeyedWattPilot { Key = "wp", Device = new WattPilot { DeviceName = "Zoe", PowerTotal = 3700 } });

        ((IPagePresenter)presenter).Show<PowerFlowView>(PowerFlowView.PageKey, Title, _ => { });
        await HeadlessAvalonia.SettleAsync();
        return service;
    }

    private static KeyedGen24System Inverter(string key, double mppt1, double mppt2, double ac, double? storage = null, double? soc = null) => new()
    {
        Key = key,
        Device = new Gen24System
        {
            Sensors = new Gen24Sensors
            {
                Inverter = new Gen24Inverter { Solar1Power = mppt1, Solar2Power = mppt2 },
                PowerFlow = new Gen24PowerFlow { SolarPower = mppt1 + mppt2, InverterAcPower = ac, StoragePower = storage ?? 0 },
                Storage = storage is null ? null : new Gen24Storage { StateOfCharge = soc },
            },
        },
    };

    private static KeyedFritzBoxDevice Plug(string key, string name, double watts) => new()
    {
        Key = key,
        Device = new FritzBoxDevice { DisplayName = name, Features = FritzBoxFeatures.PowerMeter, PowerMeter = new FritzBoxPowerMeter { PowerWatts = watts } },
    };

    private static ChildWindow Window => HeadlessAvalonia.Windows.OfType<ChildWindow>().Single(window => window.Title == Title);

    private static PowerFlowView Body => Assert.IsType<PowerFlowView>(Window.HostedContent);

    private static PowerFlowViewModel ViewModel => Assert.IsType<PowerFlowViewModel>(Body.DataContext);

    private static IReadOnlyList<Border> Cards => Body.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("Card")).ToList();

    private static Border CardOf(string key) => Cards.Single(border => border.DataContext is PowerFlowNodeItem { Key: var k } && k == key);

    private static IReadOnlyList<Avalonia.Controls.Shapes.Path> WirePaths => Body.Wires.Children.OfType<Avalonia.Controls.Shapes.Path>().ToList();

    [Fact]
    public Task The_page_opens_at_its_declared_size_with_a_card_per_node_and_a_wire_to_every_one() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var window = Window;
        Assert.InRange(window.Width, 1000, 1500);
        Assert.True(window.CanResize);

        // Grid, house, inverter A with two trackers and a battery, inverter B with two trackers, three consumers,
        // the rest of the house.
        Assert.Equal(1 + 1 + 4 + 3 + 3 + 1, Cards.Count);
        Assert.NotNull(CardOf(PowerFlowSnapshot.GridKey));
        Assert.NotNull(CardOf(PowerFlowSnapshot.RestOfHouseKey));
        Assert.NotNull(CardOf("inv-a/mppt2"));
        Assert.NotNull(CardOf("inv-a/battery"));
        Assert.Equal(PowerFlowNodeKind.Car, ((PowerFlowNodeItem)CardOf("wp").DataContext!).Node.Kind);

        // Every wire is a trace and a flow path: five DC wires, two AC wires, the grid, the trunk, the house, the
        // spine, at least one rail and a stub per consumer card.
        var paths = WirePaths;
        Assert.True(paths.Count >= 2 * (5 + 2 + 1 + 1 + 1 + 1 + 1 + 4), $"{paths.Count} wire paths");
        Assert.Equal(0, paths.Count % 2);

        // An idle wire is a connector too: the trace of every wire, the fridge's and the second tracker's
        // included, has a stroke and a thickness from the first layout on. It did not, once.
        Assert.All(paths.Where(path => path.IsVisible), path =>
        {
            Assert.NotNull(path.Stroke);
            Assert.True(path.StrokeThickness > 0, "a wire without a thickness");
        });

        Window.Close();
    });

    [Fact]
    public Task A_reading_updates_the_card_and_never_rebuilds_it() => HeadlessAvalonia.RunAsync(async () =>
    {
        var service = await StartAsync();

        var card = CardOf("hp");
        var item = (PowerFlowNodeItem)card.DataContext!;
        var paths = WirePaths.Count;
        Assert.Equal(620, item.Node.Power);

        // What the hub does when the plug reports: the device is written in place and raises PropertyChanged.
        ((KeyedFritzBoxDevice)service.AllPowerConsumers[0]).Device.PowerMeter = new FritzBoxPowerMeter { PowerWatts = 800 };
        await HeadlessAvalonia.SettleAsync();

        Assert.Same(card, CardOf("hp"));
        Assert.Same(item, card.DataContext);
        Assert.Equal(800, item.Node.Power);
        Assert.Contains(card.GetVisualDescendants().OfType<TextBlock>(), text => text.Classes.Contains("Value") && text.Text == "800 W");
        Assert.Equal(paths, WirePaths.Count);

        // And the rest of the house followed: 5100 less 800, 0 and 3700.
        Assert.Equal(5100 - 800 - 3700, ((PowerFlowNodeItem)CardOf(PowerFlowSnapshot.RestOfHouseKey).DataContext!).Node.Power);

        Window.Close();
    });

    [Fact]
    public Task A_consumer_that_appears_gets_a_card_and_a_stub_of_its_own() => HeadlessAvalonia.RunAsync(async () =>
    {
        var service = await StartAsync();
        var cards = Cards.Count;
        var paths = WirePaths.Count;

        service.AllPowerConsumers.Add(Plug("tv", "Television", 110));
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(cards + 1, Cards.Count);
        Assert.NotNull(CardOf("tv"));
        // One stub more; the rails may have changed too, but never fewer paths than before.
        Assert.True(WirePaths.Count >= paths + 2, $"{WirePaths.Count} wire paths after {paths}");

        Window.Close();
    });

    [Fact]
    public Task Closing_the_page_lets_go_of_the_devices() => HeadlessAvalonia.RunAsync(async () =>
    {
        var service = await StartAsync();
        var viewModel = ViewModel;

        Window.Close();
        await HeadlessAvalonia.SettleAsync();
        Assert.DoesNotContain(HeadlessAvalonia.Windows.OfType<ChildWindow>(), window => window.Title == Title);

        var snapshot = viewModel.Snapshot;
        ((KeyedFritzBoxDevice)service.AllPowerConsumers[0]).Device.PowerMeter = new FritzBoxPowerMeter { PowerWatts = 1 };
        service.AllPowerConsumers.Add(Plug("late", "Late", 1));
        await HeadlessAvalonia.SettleAsync();

        Assert.Same(snapshot, viewModel.Snapshot);
    });
}
