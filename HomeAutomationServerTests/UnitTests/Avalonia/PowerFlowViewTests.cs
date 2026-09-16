using Avalonia.Controls;
using Avalonia.VisualTree;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;
using De.Hochstaetter.HomeAutomationClient.Views;
using De.Hochstaetter.HomeAutomationClient.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The power flow page in a window of its own: a card per node, wires between them, and figures that update
/// without the cards being rebuilt. See the memory document <c>PowerFlowPage.Lifecycle</c>.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class PowerFlowViewTests
{
    private const string Title = "Power flow";

    private static async Task<(FakeUpdateService Service, PowerFlowViewModel Dialog, Task<bool> Shown)> StartAsync()
    {
        var service = new FakeUpdateService();
        var presenter = new WindowPresenter();

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IUpdateService>(service)
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter));

        var mainWindow = new Window { Title = "main", Width = 400, Height = 300 };
        mainWindow.Show();

        // Two inverters, one of them with a battery, and three metered consumers: a heat pump, a fridge, a car.
        service.Inverters.Add(Inverter("inv-a", 3620, 2410, storage: -1120, soc: 0.68));
        service.Inverters.Add(Inverter("inv-b", 1010, 950));
        service.SitePowerFlow.LoadPower = -5100;
        service.SitePowerFlow.InverterAcPower = 3360;
        service.SitePowerFlow.GridPower = 1740;
        service.SitePowerFlow.SolarPower = 4630;
        service.SitePowerFlow.StoragePower = -1120;
        service.AllPowerConsumers.Add(Plug("hp", "Heat pump", 620));
        service.AllPowerConsumers.Add(Plug("fridge", "Fridge", 95));
        service.AllPowerConsumers.Add(new KeyedWattPilot { Key = "wp", Device = new WattPilot { DeviceName = "Zoe", PowerTotal = 3700 } });

        var dialog = new PowerFlowViewModel(new DialogParameters { Title = Title, IsResizeable = true });
        var shown = dialog.ShowDialogAsync();
        await HeadlessAvalonia.SettleAsync();
        return (service, dialog, shown);
    }

    private static KeyedGen24System Inverter(string key, double solar, double ac, double? storage = null, double? soc = null) => new()
    {
        Key = key,
        Device = new Gen24System
        {
            Sensors = new Gen24Sensors
            {
                PowerFlow = new Gen24PowerFlow { SolarPower = solar, InverterAcPower = ac, StoragePower = storage ?? 0 },
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

    private static IReadOnlyList<Border> Cards => Body.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("Card")).ToList();

    private static Border CardOf(string key) => Cards.Single(border => border.DataContext is PowerFlowNodeItem { Key: var k } && k == key);

    private static int WirePaths => Body.Wires.Children.OfType<Avalonia.Controls.Shapes.Path>().Count();

    [Fact]
    public Task The_page_opens_at_its_declared_size_with_a_card_per_node_and_the_wires_between() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (_, dialog, shown) = await StartAsync();

        var window = Window;
        var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary ?? window.Screens.All.Single();
        Assert.Equal(Math.Min(1500, screen.WorkingArea.Width / screen.Scaling), window.Width);
        Assert.True(window.CanResize);

        // Grid, house, inverter A with panels and battery, inverter B with panels, three consumers, the rest of the house.
        Assert.Equal(1 + 1 + 3 + 2 + 3 + 1, Cards.Count);
        Assert.NotNull(CardOf(PowerFlowSnapshot.GridKey));
        Assert.NotNull(CardOf(PowerFlowSnapshot.RestOfHouseKey));
        Assert.Equal(PowerFlowNodeKind.Car, ((PowerFlowNodeItem)CardOf("wp").DataContext!).Node.Kind);

        // Every wire is a trace and a flow path: two DC wires and two AC wires for the inverters, one more DC wire for
        // the battery, the grid, the trunk, the house, the spine, at least one rail and a stub per consumer card.
        Assert.True(WirePaths >= 2 * (2 + 2 + 1 + 1 + 1 + 1 + 1 + 1 + 4), $"{WirePaths} wire paths");
        Assert.Equal(0, WirePaths % 2);

        await dialog.AbortAsync();
        await shown;
    });

    [Fact]
    public Task A_reading_updates_the_card_and_never_rebuilds_it() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (service, dialog, shown) = await StartAsync();

        var card = CardOf("hp");
        var item = (PowerFlowNodeItem)card.DataContext!;
        var paths = WirePaths;
        Assert.Equal(620, item.Node.Power);

        // What the hub does when the plug reports: the device is written in place and raises PropertyChanged.
        ((KeyedFritzBoxDevice)service.AllPowerConsumers[0]).Device.PowerMeter = new FritzBoxPowerMeter { PowerWatts = 800 };
        await HeadlessAvalonia.SettleAsync();

        Assert.Same(card, CardOf("hp"));
        Assert.Same(item, card.DataContext);
        Assert.Equal(800, item.Node.Power);
        Assert.Contains(card.GetVisualDescendants().OfType<TextBlock>(), text => text.Classes.Contains("Value") && text.Text == "800 W");
        Assert.Equal(paths, WirePaths);

        // And the rest of the house followed: 5100 less 800, 95 and 3700.
        Assert.Equal(5100 - 800 - 95 - 3700, ((PowerFlowNodeItem)CardOf(PowerFlowSnapshot.RestOfHouseKey).DataContext!).Node.Power);

        await dialog.AbortAsync();
        await shown;
    });

    [Fact]
    public Task A_consumer_that_appears_gets_a_card_and_a_stub_of_its_own() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (service, dialog, shown) = await StartAsync();
        var cards = Cards.Count;
        var paths = WirePaths;

        service.AllPowerConsumers.Add(Plug("tv", "Television", 110));
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(cards + 1, Cards.Count);
        Assert.NotNull(CardOf("tv"));
        // One stub more; the rails may have changed too, but never fewer paths than before.
        Assert.True(WirePaths >= paths + 2, $"{WirePaths} wire paths after {paths}");

        await dialog.AbortAsync();
        await shown;
    });

    [Fact]
    public Task Closing_the_page_lets_go_of_the_devices() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (service, dialog, shown) = await StartAsync();

        await dialog.AbortAsync();
        await shown;
        await HeadlessAvalonia.SettleAsync();
        Assert.DoesNotContain(HeadlessAvalonia.Windows.OfType<ChildWindow>(), window => window.Title == Title);

        var snapshot = dialog.Snapshot;
        ((KeyedFritzBoxDevice)service.AllPowerConsumers[0]).Device.PowerMeter = new FritzBoxPowerMeter { PowerWatts = 1 };
        service.AllPowerConsumers.Add(Plug("late", "Late", 1));
        await HeadlessAvalonia.SettleAsync();

        Assert.Same(snapshot, dialog.Snapshot);
    });
}
