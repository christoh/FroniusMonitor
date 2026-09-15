using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using De.Hochstaetter.HomeAutomationClient.Controls;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// Pinch to zoom, which unlike the wheel and the keyboard is restricted to what the fingers are on. So these are
/// as much about what does <b>not</b> zoom as about what does.
/// </summary>
/// <remarks>
/// The gesture is synthesized, because the headless platform has no two-finger input: the routed events the
/// recognizer would raise are raised here instead. That covers everything above the recognizer - the baseline, the
/// arithmetic, the limits and the restriction - and nothing below it. Whether real fingers reach the recognizer
/// through the scroll viewer around the gauges is for a touch screen to say.
/// </remarks>
[Collection(AvaloniaCollection.Name)]
public sealed class ZoomPinchTests
{
    private static void Pinch(Interactive element, double scale) =>
        element.RaiseEvent(new PinchEventArgs(scale, new Point(0, 0)) { RoutedEvent = InputElement.PinchEvent });

    private static void PinchEnded(Interactive element) =>
        element.RaiseEvent(new PinchEndedEventArgs { RoutedEvent = InputElement.PinchEndedEvent });

    private sealed record Fixture(Window Window, ZoomBox Box, Border Content, Border Elsewhere, StackPanel Root);

    private static async Task<Fixture> StartAsync()
    {
        HeadlessAvalonia.Reset();

        var content = new Border { Width = 100, Height = 50, Background = Brushes.Green };
        var box = new ZoomBox { Child = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var elsewhere = new Border { Width = 200, Height = 150, Background = Brushes.Gray };
        var root = new StackPanel { Children = { box, elsewhere } };
        var window = new Window { Width = 400, Height = 300, Content = root };
        ZoomBox.SetIsScope(window, true);
        window.Show();
        await HeadlessAvalonia.SettleAsync();
        return new Fixture(window, box, content, elsewhere, root);
    }

    [Fact]
    public Task A_pinch_follows_the_fingers_from_where_they_landed() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();

        Pinch(fixture.Content, 1.5);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(1.5, fixture.Box.Scale, 9);

        // A pinch reports how far apart the fingers are relative to where they started, not a step, so the scale
        // follows them rather than compounding.
        Pinch(fixture.Content, 2);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(2, fixture.Box.Scale, 9);

        Pinch(fixture.Content, 1);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(1, fixture.Box.Scale, 9);
    });

    [Fact]
    public Task The_next_gesture_starts_where_the_last_one_left_off() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();

        Pinch(fixture.Content, 2);
        PinchEnded(fixture.Content);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(2, fixture.Box.Scale, 9);

        Pinch(fixture.Content, 1.5);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(3, fixture.Box.Scale, 9);
        PinchEnded(fixture.Content);
    });

    [Fact]
    public Task A_pinch_cannot_pass_the_limits_either() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();

        Pinch(fixture.Content, 100);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(fixture.Box.MaximumScale, fixture.Box.Scale, 9);
        PinchEnded(fixture.Content);
    });

    [Fact]
    public Task A_pinch_zooms_only_what_the_fingers_are_on() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();

        // Outside every zoom box: it may well be meant as a scroll gesture, so nothing zooms.
        Pinch(fixture.Elsewhere, 2);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(1, fixture.Box.Scale, 9);
        PinchEnded(fixture.Elsewhere);

        // And it does not spread to the other boxes of the window, unlike the wheel.
        var second = new ZoomBox { Child = new Border { Width = 40, Height = 40, Background = Brushes.Blue } };
        fixture.Root.Children.Add(second);
        await HeadlessAvalonia.SettleAsync();

        Pinch(fixture.Content, 2);
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(2, fixture.Box.Scale, 9);
        Assert.Equal(1, second.Scale, 9);
        PinchEnded(fixture.Content);
    });
}
