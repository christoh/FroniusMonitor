using Avalonia;
using Avalonia.Controls;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Controls;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The "Always fully color gauges" switch of the main view, and whether the gauges of a detail view follow it
/// from a window of their own - which is where the desktop head puts a detail page, and where the switch did
/// nothing at all until 2026-09-19 because the binding looked for <c>MainView</c> above the gauge.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class GaugeColoringTests
{
    /// <summary>
    /// A window with nothing above it, holding one gauge in the <c>GaugeGroups</c> panel a detail view puts its
    /// groups in. This is the desktop's detail page as far as the style is concerned: no main view anywhere.
    /// </summary>
    private static HalfCircleGauge ShowGaugeInItsOwnWindow()
    {
        var gauge = new HalfCircleGauge();
        var window = new Window { Content = new WrapPanel { Classes = { "GaugeGroups" }, Children = { gauge } } };
        window.Show();
        return gauge;
    }

    private static MainViewModel Start()
    {
        var presenter = new WindowPresenter();

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter));

        return HeadlessAvalonia.CreateMainViewModel(presenter, presenter);
    }

    [Fact]
    public Task A_gauge_in_a_window_of_its_own_follows_the_switch() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();
        var gauge = ShowGaugeInItsOwnWindow();
        await HeadlessAvalonia.SettleAsync();

        // The switch is on when the app comes up, so the gauges start fully colored.
        Assert.True(mainViewModel.ColorAllTicks);
        Assert.True(gauge.ColorAllTicks);

        mainViewModel.ColorAllTicks = false;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(gauge.ColorAllTicks);

        mainViewModel.ColorAllTicks = true;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(gauge.ColorAllTicks);
    });

    /// <summary>A window opened after the switch was thrown starts out the way the switch stands.</summary>
    [Fact]
    public Task A_gauge_that_opens_later_starts_the_way_the_switch_stands() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();
        mainViewModel.ColorAllTicks = false;

        var gauge = ShowGaugeInItsOwnWindow();
        await HeadlessAvalonia.SettleAsync();

        Assert.False(gauge.ColorAllTicks);
    });

    /// <summary>
    /// The switch reaches every window through an application resource, which is the one thing they share. A
    /// test may read it, but nothing outside <see cref="MainViewModel"/> may write it.
    /// </summary>
    [Fact]
    public Task The_switch_is_published_as_an_application_resource() => HeadlessAvalonia.RunAsync(async () =>
    {
        var mainViewModel = Start();
        await HeadlessAvalonia.SettleAsync();

        Assert.True(Application.Current!.Resources.TryGetResource(MainViewModel.ColorAllTicksResourceKey, null, out var value));
        Assert.Equal(true, value);

        mainViewModel.ColorAllTicks = false;
        Assert.True(Application.Current.Resources.TryGetResource(MainViewModel.ColorAllTicksResourceKey, null, out value));
        Assert.Equal(false, value);
    });
}
