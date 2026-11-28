using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Controls;
using De.Hochstaetter.HomeAutomationClient.Models;
using De.Hochstaetter.HomeAutomationClient.Services;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using De.Hochstaetter.HomeAutomationClient.Views;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// The fingers on the empty space of a box are on the box. The recognizer sits on the box, and it only sees
    /// pointers whose hit test lands on the box or inside it - and Avalonia hits only what has been drawn, so until
    /// 2026-11 the space between the content of a box was not the box at all: on an iPad, pinching worked with the
    /// fingers on a card or a gauge and did nothing between them, which on the power flow page is most of it.
    /// </summary>
    [Fact]
    public Task The_empty_space_of_a_box_takes_the_fingers_as_well() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();

        var content = new Border { Width = 100, Height = 50, Background = Brushes.Green, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var box = new ZoomBox { Child = content, Width = 300, Height = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = new Window { Width = 400, Height = 300, Content = box };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        Assert.Same(content, window.InputHitTest(new Point(50, 25)));
        Assert.Same(box, window.InputHitTest(new Point(250, 150)));

        // Beyond the box it is something else, as before.
        Assert.NotSame(box, window.InputHitTest(new Point(350, 250)));

        // And the area follows the box when it changes size, as a window being resized makes it.
        box.Width = 380;
        box.Height = 280;
        await HeadlessAvalonia.SettleAsync();
        Assert.Same(box, window.InputHitTest(new Point(350, 250)));
        window.Close();
    });

    /// <summary>
    /// The dashboard's box is the area below the power flow, all of it: it stretches over the scroll viewer, so a
    /// pinch beside the cards is on it too, while the cards stay on its left as they always were.
    /// </summary>
    [Fact]
    public Task The_dashboard_takes_a_pinch_anywhere_below_the_power_flow() => HeadlessAvalonia.RunAsync(async () =>
    {
        var service = new FakeUpdateService();
        var presenter = new WindowPresenter();

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IUpdateService>(service)
            .AddSingleton<IWebClientService, WebClientService>()
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter)
            .AddSingleton(_ => HeadlessAvalonia.CreateMainViewModel(presenter, presenter))
            .AddSingleton<DashboardViewModel>()
            .AddTransient<DashboardView>());

        for (var i = 0; i < 3; i++)
        {
            service.AllPowerConsumers.Add(new KeyedFritzBoxDevice
            {
                Key = $"plug-{i}",
                Device = new FritzBoxDevice { DisplayName = $"Plug {i}", Features = FritzBoxFeatures.PowerMeter | FritzBoxFeatures.AcOutlet, IsPresent = true, PowerMeter = new FritzBoxPowerMeter { PowerWatts = 10 * i } },
            });
        }

        var view = IoC.GetRegistered<DashboardView>();
        var window = new Window { Width = 1400, Height = 900, Content = view };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        var items = view.FindControl<ItemsControl>("PowerConsumerItemsControl")!;
        var box = items.FindAncestorOfType<ZoomBox>()!;
        var scrollViewer = box.FindAncestorOfType<ScrollViewer>()!;
        Assert.Equal(3, items.GetVisualDescendants().OfType<PowerConsumer>().Count());

        Assert.Equal(scrollViewer.Viewport.Width, box.Bounds.Width, 1);
        Assert.Equal(0, items.TranslatePoint(new Point(0, 0), box)!.Value.X, 1);
        Assert.True(items.Bounds.Width < box.Bounds.Width - 100, "the cards should leave room beside them for this test");

        // Beside the cards, halfway down them.
        var beside = box.TranslatePoint(new Point(box.Bounds.Width - 50, items.Bounds.Height / 2), window)!.Value;
        Assert.Same(box, window.InputHitTest(beside));
        window.Close();
    });
}
