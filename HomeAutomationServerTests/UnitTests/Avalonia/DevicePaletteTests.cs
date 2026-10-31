using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Controls;
using De.Hochstaetter.HomeAutomationClient.Converters;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The colours a device card and a gauge tile take for the state of their device come from the palette in
/// <c>App.axaml</c>, and only from there. Both tests stand for a defect found on 2026-10-31, when the light
/// palette was redone: a key the code asked for under another spelling than the palette's, and a second, hard-coded
/// copy of the palette that beat the real one.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class DevicePaletteTests
{
    /// <summary>
    /// <c>InverterBackgroundColor</c> asked for <c>DeviceOuterBackgroundStartup</c> while the palette spelled it
    /// <c>…StartUp</c>, so every gauge of a detail view lost its background while the device started up.
    /// </summary>
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public Task A_gauge_of_a_starting_device_has_the_palettes_startup_colour(string variantName) => HeadlessAvalonia.RunAsync(() =>
    {
        HeadlessAvalonia.Reset();
        var application = Application.Current!;
        var requested = application.RequestedThemeVariant;
        var variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        try
        {
            application.RequestedThemeVariant = variant;
            var palette = (ResourceDictionary)application.Resources.ThemeDictionaries[variant];

            var brush = new InverterBackgroundColor().Convert(["STATE_STARTUP", 0d, false], typeof(IBrush), null, CultureInfo.InvariantCulture);

            Assert.NotNull(brush);
            Assert.Same(palette["DeviceOuterBackgroundStartup"], brush);
        }
        finally
        {
            application.RequestedThemeVariant = requested;
        }

        return Task.CompletedTask;
    });

    /// <summary>
    /// <c>SmartMeterControl</c> painted its card twice: from the palette in its code behind, and through a binding
    /// to a converter with a hard-coded copy of the WPF colours. The binding won, so the light palette never
    /// reached the smart meter's card - which nobody saw for as long as both copies held the same colours.
    /// </summary>
    [Fact]
    public Task The_smart_meter_card_takes_the_colour_of_its_state_from_the_palette() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        var running = new SolidColorBrush(Colors.Magenta);
        var meter = new SmartMeterControl { OuterRunning = running };
        var window = new Window { Content = meter };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        meter.MeterStatus = new Gen24Status { StatusCode = "STATE_RUNNING", Status = 1 };
        await HeadlessAvalonia.SettleAsync();

        Assert.Same(running, meter.FindControl<Border>("BackgroundProvider")!.Background);
        window.Close();
    });
}
