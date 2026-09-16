using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using De.Hochstaetter.HomeAutomationClient.Controls;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// Ctrl with the wheel and with plus, minus and zero: what they do, where they may be done, and that what they do
/// is a layout change rather than a painted one. See the memory document <c>Zoom</c>.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class ZoomBoxTests
{
    /// <summary>Far from the zoom box and over another control, because the gesture must not need the pointer on it.</summary>
    private static readonly Point awayFromTheBox = new(150, 250);

    private sealed record Fixture(Window Window, ZoomBox Box, Border Content, Border Elsewhere, StackPanel Root);

    /// <summary>
    /// A window marked as a scope, with a zoom box and a larger control beside it. The box is aligned left and top
    /// so that it is arranged at exactly the size it asks for and its bounds are the measurement.
    /// </summary>
    private static async Task<Fixture> StartAsync(bool scopeOnTheWindow = true)
    {
        HeadlessAvalonia.Reset();

        var content = new Border { Width = 100, Height = 50, Background = Brushes.Green };
        var box = new ZoomBox { Child = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var elsewhere = new Border { Width = 300, Height = 200, Background = Brushes.Gray };
        var root = new StackPanel { Children = { box, elsewhere } };
        var window = new Window { Width = 500, Height = 400, Content = root };

        if (scopeOnTheWindow)
        {
            ZoomBox.SetIsScope(window, true);
        }

        window.Show();
        await HeadlessAvalonia.SettleAsync();
        return new Fixture(window, box, content, elsewhere, root);
    }

    private static async Task WheelAsync(Window window, double delta, RawInputModifiers modifiers = RawInputModifiers.Control)
    {
        window.MouseWheel(awayFromTheBox, new Vector(0, delta), modifiers);
        await HeadlessAvalonia.SettleAsync();
    }

    private static async Task KeyAsync(Window window, Key key, PhysicalKey physicalKey, string symbol, RawInputModifiers modifiers = RawInputModifiers.Control)
    {
        window.KeyPress(key, modifiers, physicalKey, symbol);
        await HeadlessAvalonia.SettleAsync();
    }

    [Fact]
    public Task Ctrl_and_the_wheel_zoom_from_anywhere_in_the_window() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();
        var unzoomedWidth = fixture.Box.Bounds.Width;

        Assert.Equal(1, fixture.Box.Scale);
        Assert.Equal(100, unzoomedWidth, 1);

        await WheelAsync(fixture.Window, 1);

        Assert.True(fixture.Box.Scale > 1);

        // Laid out bigger, not just painted bigger: the box grew and its child kept its own size.
        Assert.True(fixture.Box.Bounds.Width > unzoomedWidth + 1);
        Assert.Equal(100, fixture.Content.Bounds.Width, 1);

        await WheelAsync(fixture.Window, -1);
        Assert.Equal(1, fixture.Box.Scale, 9);
    });

    [Fact]
    public Task The_wheel_without_Ctrl_is_scrolling_and_not_zooming() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();
        await WheelAsync(fixture.Window, 1, RawInputModifiers.None);
        Assert.Equal(1, fixture.Box.Scale);
    });

    [Fact]
    public Task Plus_minus_and_zero_zoom_on_both_the_number_row_and_the_number_pad() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();

        await KeyAsync(fixture.Window, Key.OemPlus, PhysicalKey.Equal, "+");
        Assert.Equal(ZoomBox.DefaultStep, fixture.Box.Scale, 9);

        await KeyAsync(fixture.Window, Key.OemMinus, PhysicalKey.Minus, "-");
        Assert.Equal(1, fixture.Box.Scale, 9);

        await KeyAsync(fixture.Window, Key.Add, PhysicalKey.NumPadAdd, "+");
        await KeyAsync(fixture.Window, Key.Add, PhysicalKey.NumPadAdd, "+");
        Assert.Equal(ZoomBox.DefaultStep * ZoomBox.DefaultStep, fixture.Box.Scale, 9);

        await KeyAsync(fixture.Window, Key.D0, PhysicalKey.Digit0, "0");
        Assert.Equal(ZoomBox.DefaultScale, fixture.Box.Scale, 9);

        // Without Ctrl it is not a zoom.
        await KeyAsync(fixture.Window, Key.OemPlus, PhysicalKey.Equal, "+", RawInputModifiers.None);
        Assert.Equal(1, fixture.Box.Scale, 9);
    });

    [Fact]
    public Task The_gestures_work_with_the_scope_on_a_view_inside_the_window() => HeadlessAvalonia.RunAsync(async () =>
    {
        // How the app marks it: MainView and the pages are inside the window, not the window itself. A handler on
        // the scope element itself would never run - a key tunnels to whatever has the focus, and with nothing
        // focused that route stops at the top level.
        HeadlessAvalonia.Reset();

        var content = new Border { Width = 100, Height = 50, Background = Brushes.Green };
        var box = new ZoomBox { Child = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var typing = new TextBox { Width = 120 };
        var view = new UserControl { Content = new StackPanel { Children = { box, typing, new Border { Width = 200, Height = 150, Background = Brushes.Gray } } } };
        var window = new Window { Width = 400, Height = 300, Content = view };
        ZoomBox.SetIsScope(view, true);
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        window.MouseWheel(new Point(100, 250), new Vector(0, 1), RawInputModifiers.Control);
        await HeadlessAvalonia.SettleAsync();
        Assert.True(box.Scale > 1);

        box.Reset();
        window.KeyPress(Key.OemPlus, RawInputModifiers.Control, PhysicalKey.Equal, "+");
        await HeadlessAvalonia.SettleAsync();
        Assert.True(box.Scale > 1);

        // And the gesture wins over the focused control without taking the keys that control is there to receive.
        box.Reset();
        typing.Focus();
        await HeadlessAvalonia.SettleAsync();
        window.KeyTextInput("7");
        await HeadlessAvalonia.SettleAsync();
        Assert.Equal("7", typing.Text);

        window.KeyPress(Key.OemPlus, RawInputModifiers.Control, PhysicalKey.Equal, "+");
        await HeadlessAvalonia.SettleAsync();
        Assert.True(box.Scale > 1);
        Assert.Equal("7", typing.Text);
    });

    [Fact]
    public Task The_scale_stops_at_the_limits() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();

        for (var turn = 0; turn < 60; turn++)
        {
            fixture.Window.MouseWheel(awayFromTheBox, new Vector(0, 1), RawInputModifiers.Control);
        }

        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(fixture.Box.MaximumScale, fixture.Box.Scale, 9);

        for (var turn = 0; turn < 120; turn++)
        {
            fixture.Window.MouseWheel(awayFromTheBox, new Vector(0, -1), RawInputModifiers.Control);
        }

        await HeadlessAvalonia.SettleAsync();
        Assert.Equal(fixture.Box.MinimumScale, fixture.Box.Scale, 9);
    });

    [Fact]
    public Task A_view_may_set_its_own_steps_and_a_step_that_cannot_be_used_falls_back() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();

        Assert.Equal(ZoomBox.DefaultStep, fixture.Box.WheelStep);
        Assert.Equal(ZoomBox.DefaultStep, fixture.Box.KeyStep);

        fixture.Box.KeyStep = 2;
        fixture.Box.WheelStep = 1.5;

        await KeyAsync(fixture.Window, Key.OemPlus, PhysicalKey.Equal, "+");
        Assert.Equal(2, fixture.Box.Scale, 9);

        await WheelAsync(fixture.Window, 1);
        Assert.Equal(3, fixture.Box.Scale, 9);

        // Multiplied and divided by, so a zero would zoom to infinity in one direction and do nothing in the other.
        fixture.Box.KeyStep = 0;
        Assert.Equal(ZoomBox.DefaultStep, fixture.Box.KeyStep);
    });

    [Fact]
    public Task Every_zoom_box_of_one_scope_follows_the_wheel() => HeadlessAvalonia.RunAsync(async () =>
    {
        var fixture = await StartAsync();
        var second = new ZoomBox { Child = new Border { Width = 40, Height = 40 } };
        fixture.Root.Children.Add(second);
        await HeadlessAvalonia.SettleAsync();

        await WheelAsync(fixture.Window, 1);

        Assert.True(second.Scale > 1);
        Assert.Equal(fixture.Box.Scale, second.Scale, 9);
    });

    [Fact]
    public Task A_scroll_viewer_around_a_zoom_box_gets_more_to_scroll() => HeadlessAvalonia.RunAsync(async () =>
    {
        // Which is what keeps the bottom of a zoomed gauge group reachable.
        var fixture = await StartAsync();
        fixture.Root.Children.Remove(fixture.Box);
        var scroller = new ScrollViewer { Height = 80, Content = fixture.Box };
        fixture.Root.Children.Insert(0, scroller);
        await HeadlessAvalonia.SettleAsync();

        var extentBefore = scroller.Extent.Height;
        await WheelAsync(fixture.Window, 1);
        await WheelAsync(fixture.Window, 1);

        Assert.True(scroller.Extent.Height > extentBefore + 1);
    });

    [Fact]
    public Task A_scope_with_nothing_to_zoom_leaves_the_wheel_to_the_scroll_viewer() => HeadlessAvalonia.RunAsync(async () =>
    {
        // This is what lets a dialog window carry the same scope and still scroll.
        HeadlessAvalonia.Reset();

        var scroller = new ScrollViewer { Content = new Border { Height = 2000, Background = Brushes.Gray } };
        var window = new Window { Width = 200, Height = 200, Content = scroller };
        ZoomBox.SetIsScope(window, true);
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        window.MouseWheel(new Point(100, 100), new Vector(0, -1));
        await HeadlessAvalonia.SettleAsync();

        Assert.True(scroller.Offset.Y > 0);
    });
}
