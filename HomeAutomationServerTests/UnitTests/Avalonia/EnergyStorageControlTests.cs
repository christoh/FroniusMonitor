using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Controls;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The battery of the dashboard while it charges: the frame pulses towards <c>BatteryBorderCharging</c> as it does
/// in WPF, the plus pole goes along with it, and both return to the traffic light colour once charging stops.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class EnergyStorageControlTests
{
    private const double Voltage = 400;

    // Never reported by the storage here, so the traffic light is unknown and the frame takes this one.
    private static readonly ISolidColorBrush trafficLight = Brushes.Gold;

    private static (EnergyStorageControl Control, Gen24Storage Storage) Show(double current)
    {
        HeadlessAvalonia.Reset();
        var storage = new Gen24Storage { VoltageOuter = Voltage, Current = current };

        var control = new EnergyStorageControl
        {
            StatusGreen = Brushes.DarkGreen,
            StatusYellow = trafficLight,
            StatusRed = Brushes.Red,
            Gen24System = new Gen24System { Sensors = new Gen24Sensors { Storage = storage } },
        };

        new Window { Content = control }.Show();
        return (control, storage);
    }

    private static Color FrameColor(EnergyStorageControl control) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(control.Enclosure.BorderBrush).Color;

    // The animation clock of the headless platform runs on real time, a forced tick only makes it look at the
    // clock. So time has to pass between two ticks; 300 ms is a fifth of the 1.5 s pulse.
    private static async Task LetTheAnimationRunAsync()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await Task.Delay(300);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await HeadlessAvalonia.SettleAsync();
    }

    private static void AssertStrictlyBetween(byte from, byte to, byte actual) =>
        Assert.InRange(actual, Math.Min(from, to) + 1, Math.Max(from, to) - 1);

    [Theory]
    [InlineData(0.1, true)] // 40 W into the battery
    [InlineData(0.02, false)] // 8 W, below the threshold
    [InlineData(0, false)]
    [InlineData(-0.8, false)] // discharging
    public Task Only_charging_above_10_W_counts_as_charging(double current, bool isCharging) => HeadlessAvalonia.RunAsync(async () =>
    {
        var (control, _) = Show(current);
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(isCharging, control.Enclosure.Classes.Contains("charging"));
    });

    [Fact]
    public Task The_frame_and_the_plus_pole_pulse_while_charging() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (control, _) = Show(0.1);
        await LetTheAnimationRunAsync();

        // On its way from the traffic light to the charging colour of the theme. Only a DynamicResource in the key
        // frame that is resolved gets it there; one that is not would leave it standing or fade it out.
        Assert.True(control.TryFindResource("BatteryBorderCharging", control.ActualThemeVariant, out var resource));
        var charging = Assert.IsAssignableFrom<ISolidColorBrush>(resource).Color;
        var frame = FrameColor(control);

        Assert.Equal(byte.MaxValue, frame.A);
        AssertStrictlyBetween(trafficLight.Color.R, charging.R, frame.R);
        AssertStrictlyBetween(trafficLight.Color.G, charging.G, frame.G);
        AssertStrictlyBetween(trafficLight.Color.B, charging.B, frame.B);
        Assert.Same(control.Enclosure.BorderBrush, control.PlusPole.Background);
    });

    [Fact]
    public Task The_frame_returns_to_the_traffic_light_when_charging_stops() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (control, storage) = Show(0.1);
        await LetTheAnimationRunAsync();

        storage.Current = -0.8;
        await LetTheAnimationRunAsync();

        Assert.DoesNotContain("charging", control.Enclosure.Classes);
        Assert.Equal(trafficLight.Color, FrameColor(control));
        Assert.Same(control.Enclosure.BorderBrush, control.PlusPole.Background);
    });
}
