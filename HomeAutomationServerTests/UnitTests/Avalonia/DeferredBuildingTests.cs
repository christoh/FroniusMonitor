using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.HomeAutomationClient.Controls;
using De.Hochstaetter.HomeAutomationClient.Services;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the dashboard builds when, since 2026-10-31: a panel of a device card only when it is shown
/// (<see cref="LazyPanel"/>), a consumer card a frame after the page (<see cref="DeferredContent"/>), and a gauge
/// that is not on screen takes its value without the animation. Before, the page was built whole in one layout
/// pass - 2900 visuals, 250 gauges of which ten could be seen - and the app stood still until it was done.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class DeferredBuildingTests
{
    private static FuncTemplate<Control> Marker(string text) => new(() => new TextBlock { Text = text });

    [Fact]
    public Task A_hidden_lazy_panel_builds_nothing_until_it_is_shown() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        var panel = new LazyPanel { Template = Marker("built"), IsVisible = false };
        var window = new Window { Content = panel, DataContext = "the data context" };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        Assert.False(panel.IsBuilt);

        panel.IsVisible = true;
        await HeadlessAvalonia.SettleAsync();

        var child = Assert.IsType<TextBlock>(panel.Child);
        Assert.Equal("built", child.Text);

        // The child is where the panel is in the tree, so it sees what the panel sees.
        Assert.Equal("the data context", child.DataContext);

        // Hidden again, it keeps what it built: switching back is as quick as it always was.
        panel.IsVisible = false;
        await HeadlessAvalonia.SettleAsync();
        Assert.Same(child, panel.Child);

        panel.IsVisible = true;
        await HeadlessAvalonia.SettleAsync();
        Assert.Same(child, panel.Child);
        window.Close();
    });

    [Fact]
    public Task Deferred_content_is_built_after_the_layout_pass_that_placed_it() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        var contents = Enumerable.Range(0, 5).Select(i => new DeferredContent { Template = Marker($"card {i}") }).ToList();
        var window = new Window { Content = new StackPanel { Children = { contents[0], contents[1], contents[2], contents[3], contents[4] } } };
        window.Show();

        // The pass that puts the page on screen places the cards and builds none of them.
        window.UpdateLayout();
        Assert.All(contents, c => Assert.False(c.IsBuilt));

        await HeadlessAvalonia.SettleAsync();
        Assert.All(contents, c => Assert.True(c.IsBuilt));
        Assert.Equal("card 3", Assert.IsType<TextBlock>(contents[3].Child).Text);
        window.Close();
    });

    [Fact]
    public Task Deferred_content_that_was_taken_down_before_its_turn_is_built_when_it_comes_back() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        var content = new DeferredContent { Template = Marker("card") };
        var window = new Window { Content = content };
        window.Show();
        window.UpdateLayout();

        window.Content = null;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(content.IsBuilt);

        window.Content = content;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(content.IsBuilt);
        window.Close();
    });

    /// <summary>
    /// The inverter card on the dashboard has twenty-five panels. Only the one its mode shows is built, and the
    /// first switch to another builds that one - with the bindings inside the template finding the inverter.
    /// </summary>
    [Fact]
    public Task An_inverter_card_builds_the_panel_it_shows_and_the_next_when_it_is_switched_to() => HeadlessAvalonia.RunAsync(async () =>
    {
        // The card reads the web client in a static field.
        HeadlessAvalonia.Reset(services => services.AddSingleton<IWebClientService, WebClientService>());

        var inverter = new Gen24System
        {
            Config = new Gen24Config { InverterSettings = new Gen24InverterSettings { SystemName = "Dach" }, MaxAcPower = 10000 },
            Sensors = new Gen24Sensors
            {
                InverterStatus = new Gen24Status { StatusCode = "STATE_RUNNING", Status = 1 },
                Inverter = new Gen24Inverter { AcVoltageL1 = 233.1, AcVoltageL2 = 231.8, AcVoltageL3 = 232.6, Solar1Power = 4120, Solar2Power = 3080 },
            },
        };

        var card = new InverterControl { Inverter = inverter, Height = 320 };
        var window = new Window { Content = card };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        var panels = card.GetVisualDescendants().OfType<LazyPanel>().ToList();
        Assert.Equal(25, panels.Count);
        Assert.Single(panels, p => p.IsBuilt);
        Assert.InRange(card.GetVisualDescendants().OfType<Gauge>().Count(), 1, 10);

        card.Mode = InverterDisplayMode.AcPhaseVoltage;
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(2, panels.Count(p => p.IsBuilt));
        var shown = Assert.Single(panels, p => p.IsVisible);
        var l1 = shown.GetVisualDescendants().OfType<LinearGauge>().First(g => g.Label == "L1");
        Assert.Equal(233.1, l1.Value);
        window.Close();
    });

    /// <summary>
    /// A gauge's needle animates to a new value for a second. Not before the gauge is loaded, and not while it
    /// cannot be seen: there the value is taken at once, and nothing is started for nobody.
    /// </summary>
    [Fact]
    public Task A_gauge_that_is_not_on_screen_takes_its_value_without_the_animation() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        var gauge = new LinearGauge { Minimum = 0, Maximum = 10, Value = 5 };

        // Not loaded yet: the needle is at the value the moment it was set.
        Assert.Equal(0.5, gauge.AnimatedValue, 6);

        var window = new Window { Content = gauge };
        window.Show();
        await HeadlessAvalonia.SettleAsync();
        Assert.True(gauge.IsLoaded);

        gauge.IsVisible = false;
        await HeadlessAvalonia.SettleAsync();
        gauge.Value = 10;
        Assert.Equal(1, gauge.AnimatedValue, 6);

        // On screen, the needle moves over a second rather than jumping.
        gauge.IsVisible = true;
        await HeadlessAvalonia.SettleAsync();
        gauge.Value = 0;
        Assert.Equal(1, gauge.AnimatedValue, 6);
        window.Close();
    });
}
