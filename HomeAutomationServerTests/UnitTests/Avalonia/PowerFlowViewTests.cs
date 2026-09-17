using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using System.Globalization;
using Avalonia.VisualTree;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models;
using De.Hochstaetter.HomeAutomationClient.Services;
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

    private static async Task<FakeUpdateService> StartAsync(Action<FakeUpdateService>? configure = null)
    {
        var service = new FakeUpdateService();
        var presenter = new WindowPresenter();

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IUpdateService>(service)
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter)
            // So the page knows whether its window can be seen, as it does in the app.
            .AddSingleton<VisibilityService>()
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
        configure?.Invoke(service);

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

    private static IReadOnlyDictionary<string, (string Path, bool Moves, bool Reversed)> Wires => Body.WireStates;

    [Fact]
    public Task The_page_opens_at_its_declared_size_with_a_card_per_node_and_a_wire_to_every_one() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var window = Window;
        Assert.InRange(window.Width, 1000, 1500);
        Assert.True(window.CanResize);

        // As tall as the page once the cards are there, not as tall as the title and the legend were before the
        // view model had heard from the devices: two rows of consumers under the header are well over this.
        Assert.True(window.Height >= 600, $"window only {window.Height} tall");
        Assert.Equal(Body.Bounds.Height, window.ClientSize.Height, 1.0);

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
    public Task Every_junction_has_a_dot_and_the_dots_lie_over_the_wires() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        var children = Body.Wires.Children;
        var dots = children.OfType<Ellipse>().ToList();
        var paths = children.OfType<Avalonia.Controls.Shapes.Path>().ToList();
        Assert.NotEmpty(dots);

        // A dot is drawn as its wire is routed; the wires routed after it must not paint over it.
        Assert.All(dots, dot => Assert.True(dot.ZIndex > paths.Max(path => path.ZIndex), "a dot under the wires"));
        Assert.All(children.OfType<Border>(), label => Assert.True(label.ZIndex > dots.Max(dot => dot.ZIndex), "a label under the dots"));

        // The house's inlet meets the trunk at a dot like every tap does; the point is where the inlet starts.
        var junction = Wires["house"].Path.Split(' ')[0][1..].Split(',').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToList();
        Assert.Contains(dots, dot => Math.Abs(Canvas.GetLeft(dot) + dot.Width / 2 - junction[0]) < 0.6 && Math.Abs(Canvas.GetTop(dot) + dot.Height / 2 - junction[1]) < 0.6);

        Window.Close();
    });

    [Fact]
    public Task No_wire_passes_a_point_twice() => HeadlessAvalonia.RunAsync(async () =>
    {
        await StartAsync();

        // The spine once went from the house up to the first rail and back down past the house to the last one, so
        // its dashes ran over each other on the upper part. Now it is a stub from the house and a run to each side.
        var wires = Wires;
        var junction = wires["spine"].Path.Split(' ')[^1][1..];
        var runs = wires.Where(wire => wire.Key.StartsWith("spine:", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(runs);
        Assert.All(runs, run => Assert.Equal(junction, run.Value.Path.Split(' ')[0][1..]));

        Assert.All(wires, wire =>
        {
            var points = wire.Value.Path.Split(' ');
            Assert.Equal(points.Length, points.Select(point => point[1..]).Distinct().Count());
        });

        Window.Close();
    });

    [Fact]
    public Task The_trunk_carries_the_net_between_its_taps() => HeadlessAvalonia.RunAsync(async () =>
    {
        // Top to bottom: the grid at -50 W (feed-in), inverter A at 4200 W, the house at 5100 W, inverter B at
        // 950 W. Between the grid and inverter A run 50 W upwards; between inverter A and the house 4150 W down;
        // between the house and inverter B 950 W upwards. As one wire the trunk ran downwards from end to end.
        await StartAsync();

        var trunk = Wires.Where(wire => wire.Key.StartsWith("trunk:", StringComparison.Ordinal)).OrderBy(wire => wire.Key).Select(wire => wire.Value).ToList();
        Assert.Equal(3, trunk.Count);
        Assert.All(trunk, segment => Assert.True(segment.Moves, "a trunk segment stands still"));
        Assert.Equal([true, false, true], trunk.Select(segment => segment.Reversed));

        // The segments join end to end, top down.
        Assert.Equal(trunk[0].Path.Split(' ')[^1][1..], trunk[1].Path.Split(' ')[0][1..]);
        Assert.Equal(trunk[1].Path.Split(' ')[^1][1..], trunk[2].Path.Split(' ')[0][1..]);

        Window.Close();
    });

    [Fact]
    public Task An_inverter_charging_its_battery_from_the_bus_draws_from_the_trunk() => HeadlessAvalonia.RunAsync(async () =>
    {
        // Inverter B takes 300 W from the AC side to charge its battery, the grid imports 1200 W, inverter A makes
        // 4200 W and the house draws 5100 W. Every trunk segment now runs downwards: 1200 W from the grid, 5400 W
        // past inverter A, and 300 W below the house into inverter B, whose own wire runs back into it.
        await StartAsync(service =>
        {
            service.Inverters[1].Device.Sensors!.PowerFlow!.InverterAcPower = -300;
            service.SitePowerFlow.GridPower = 1200;
            service.SitePowerFlow.InverterAcPower = 3900;
        });

        var wires = Wires;
        var trunk = wires.Where(wire => wire.Key.StartsWith("trunk:", StringComparison.Ordinal)).OrderBy(wire => wire.Key).Select(wire => wire.Value).ToList();
        Assert.Equal(3, trunk.Count);
        Assert.All(trunk, segment => Assert.True(segment.Moves && !segment.Reversed, "a trunk segment stands still or runs upwards"));
        Assert.True(wires["ac:inv-b"] is { Moves: true, Reversed: true }, "the charging inverter's wire does not run into it");
        Assert.True(wires["ac:inv-a"] is { Moves: true, Reversed: false });
        Assert.True(wires["grid"] is { Moves: true, Reversed: false });

        Window.Close();
    });

    [Fact]
    public Task A_rail_to_lamps_of_a_few_watts_carries_flow() => HeadlessAvalonia.RunAsync(async () =>
    {
        // A lamp at 5 W and one at half a watt, nothing else: on by the consumers' rule, and so are the rail and
        // the spine to them, at 6 W all told. Once the rail was judged like a producer, idle below 10 W, and stood
        // still above a lit lamp, which then drew its power from nowhere.
        await StartAsync(service =>
        {
            ((KeyedFritzBoxDevice)service.AllPowerConsumers[0]).Device.PowerMeter!.PowerWatts = 5;
            ((KeyedFritzBoxDevice)service.AllPowerConsumers[1]).Device.PowerMeter!.PowerWatts = 0.5;
            ((KeyedWattPilot)service.AllPowerConsumers[2]).Device.PowerTotal = 0;
            service.SitePowerFlow.LoadPower = -6;
        });

        var wires = Wires;
        Assert.True(wires["stub:hp"].Moves);
        Assert.True(wires["stub:fridge"].Moves);
        Assert.False(wires["stub:wp"].Moves);
        Assert.True(wires[$"stub:{PowerFlowSnapshot.RestOfHouseKey}"].Moves);
        // Every rail, however the cards wrap: each row here draws under 10 W and over 0.2 W.
        var rails = wires.Where(wire => wire.Key.StartsWith("rail:", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(rails);
        Assert.All(rails, rail => Assert.True(rail.Value.Moves, $"{rail.Key} stands still"));
        Assert.All(wires.Where(wire => wire.Key.StartsWith("spine", StringComparison.Ordinal)), run => Assert.True(run.Value.Moves, $"{run.Key} stands still"));

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

    [Fact]
    public Task A_minimized_page_leaves_the_devices_alone_and_catches_up_when_it_is_restored() => HeadlessAvalonia.RunAsync(async () =>
    {
        var service = await StartAsync();
        Assert.True(ViewModel.IsShown);

        Window.WindowState = WindowState.Minimized;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(ViewModel.IsShown);

        // A report while nobody looks: no snapshot is built for it.
        var before = ViewModel.Snapshot;
        ((KeyedFritzBoxDevice)service.AllPowerConsumers[0]).Device.PowerMeter = new FritzBoxPowerMeter { PowerWatts = 800 };
        await HeadlessAvalonia.SettleAsync();
        Assert.Same(before, ViewModel.Snapshot);

        // Back in sight: one snapshot, with the figure the report brought.
        Window.WindowState = WindowState.Normal;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(ViewModel.IsShown);
        Assert.NotSame(before, ViewModel.Snapshot);
        Assert.Equal(800, ((PowerFlowNodeItem)CardOf("hp").DataContext!).Node.Power);
    });
}
