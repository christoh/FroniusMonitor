using Avalonia.Controls;
using De.Hochstaetter.HomeAutomationClient.Controls;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// <see cref="Gauge.DialShowsAbsoluteValue"/>: what the needle reads when the sign of the value is not what the
/// dial is about. The cos(phi) groups of the detail views are what asked for it.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class GaugeDialTests
{
    /// <summary>
    /// The needle is <see cref="Gauge.AnimatedValue"/>, a fraction of the scale, and it is animated: setting the
    /// switch runs the calculation without the animation, which is what makes this readable at once.
    /// </summary>
    [Fact]
    public Task The_dial_reads_the_amount_while_the_value_keeps_its_sign() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();

        var gauge = new HalfCircleGauge { Minimum = -1, Maximum = 1, Value = -0.9 };
        new Window { Content = gauge }.Show();
        await HeadlessAvalonia.SettleAsync();

        gauge.DialShowsAbsoluteValue = true;
        await HeadlessAvalonia.SettleAsync();

        // |-0.9| on a scale from -1 to 1 is 95% of the way up, where +0.9 is.
        Assert.Equal(0.95, gauge.AnimatedValue, 6);

        // And the value itself is untouched, which is what the read-out under the dial is built from.
        Assert.Equal(-0.9, gauge.Value);

        gauge.DialShowsAbsoluteValue = false;
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(0.05, gauge.AnimatedValue, 6);
    });

    /// <summary>A value with no sign to drop is left exactly where it was.</summary>
    [Fact]
    public Task A_positive_value_is_where_it_always_was() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();

        var gauge = new HalfCircleGauge { Minimum = -1, Maximum = 1, Value = 0.9 };
        new Window { Content = gauge }.Show();
        await HeadlessAvalonia.SettleAsync();

        gauge.DialShowsAbsoluteValue = true;
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(0.95, gauge.AnimatedValue, 6);
    });
}
