using Avalonia.Controls;
using De.Hochstaetter.HomeAutomationClient.Services;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// How the desktop head knows whether anybody can see it: from the state of its windows, none of which has to
/// register for it.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class VisibilityServiceTests
{
    private static async Task<(VisibilityService Service, Window First, Window Second, List<bool> Changes)> StartAsync()
    {
        HeadlessAvalonia.Reset();

        var first = new Window { Title = "first", Width = 300, Height = 200 };
        var second = new Window { Title = "second", Width = 300, Height = 200 };
        first.Show();
        second.Show();
        await HeadlessAvalonia.SettleAsync();

        var service = new VisibilityService();
        var changes = new List<bool>();
        service.IsAnyVisibleChanged += (_, _) => changes.Add(service.IsAnyVisible);
        return (service, first, second, changes);
    }

    [Fact]
    public Task Nothing_is_visible_only_once_every_window_is_minimized() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (service, first, second, changes) = await StartAsync();
        Assert.True(service.IsAnyVisible);

        first.WindowState = WindowState.Minimized;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(service.IsAnyVisible);
        Assert.False(service.IsVisible(first));
        Assert.True(service.IsVisible(second));
        Assert.Empty(changes);

        second.WindowState = WindowState.Minimized;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(service.IsAnyVisible);
        Assert.Equal([false], changes);

        first.WindowState = WindowState.Normal;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(service.IsAnyVisible);
        Assert.True(service.IsVisible(first));
        Assert.Equal([false, true], changes);
    });

    [Fact]
    public Task A_hidden_window_is_not_visible_and_a_shown_one_is_again() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (service, first, second, changes) = await StartAsync();

        second.Close();
        first.Hide();
        await HeadlessAvalonia.SettleAsync();
        Assert.False(service.IsAnyVisible);
        Assert.False(service.IsVisible(first));

        first.Show();
        await HeadlessAvalonia.SettleAsync();
        Assert.True(service.IsAnyVisible);
        Assert.Equal([false, true], changes);
    });

    [Fact]
    public Task Closing_the_last_visible_window_beside_a_minimized_one_is_noticed() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (service, first, second, changes) = await StartAsync();

        first.WindowState = WindowState.Minimized;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(service.IsAnyVisible);

        // The window leaves the lifetime's list without a property of its own changing; the service follows Closed for that.
        second.Close();
        await HeadlessAvalonia.SettleAsync();
        Assert.False(service.IsAnyVisible);
        Assert.Equal([false], changes);
    });

    [Fact]
    public Task Without_any_window_nothing_speaks_against_being_seen() => HeadlessAvalonia.RunAsync(async () =>
    {
        var (service, first, second, _) = await StartAsync();

        first.WindowState = WindowState.Minimized;
        second.WindowState = WindowState.Minimized;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(service.IsAnyVisible);

        // Shutting down, or a head without windows: no window says "not seen", so the answer is the safe one.
        first.Close();
        second.Close();
        await HeadlessAvalonia.SettleAsync();
        Assert.True(service.IsAnyVisible);
    });
}
