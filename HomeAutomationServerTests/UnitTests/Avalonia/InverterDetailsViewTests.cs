using Avalonia.Controls;
using Avalonia.VisualTree;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Controls;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using De.Hochstaetter.HomeAutomationClient.Views;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The inverter detail page with real gauges on the headless platform. What is under test is the one reading the
/// page decides something by: an inverter that is not synchronized to the grid reports next to no frequency, and
/// the difference to the grid is then a number of tens of thousands of mHz that means nothing.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class InverterDetailsViewTests
{
    private const string Title = "Inverter A";

    private static Gen24System Start(double? inverterFrequency, double? gridFrequency = 50)
    {
        var presenter = new WindowPresenter();

        HeadlessAvalonia.Reset(services => services
            .AddSingleton<IDialogPresenter>(presenter)
            .AddSingleton<IPagePresenter>(presenter)
            // Built when it is first asked for, which is after Reset has put the container behind IoC.
            .AddSingleton(_ => HeadlessAvalonia.CreateMainViewModel(presenter, presenter))
            .AddTransient<InverterDetailsView>()
            .AddTransient<InverterDetailsViewModel>());

        var gen24System = new Gen24System
        {
            Sensors = new Gen24Sensors
            {
                Inverter = new Gen24Inverter { InverterFrequency = inverterFrequency, FeedInPointFrequency = gridFrequency },
            },
        };

        IoC.Get<IPagePresenter>().Show<InverterDetailsView>("inverter-a", Title, view =>
        {
            view.ViewModel.Gen24System = gen24System;

            // The frequency group is one of the switches that start out off, and a group that is off is not in
            // the tree at all. This test is about the gauge inside it, so it is switched on the way a user would.
            view.ViewModel.Frequency = true;
        });
        return gen24System;
    }

    private static ChildWindow Window => HeadlessAvalonia.Windows.OfType<ChildWindow>().Single(window => window.Title == Title);

    /// <summary>The gauge by the label the user reads, so the test breaks if the page stops showing that one.</summary>
    private static HalfCircleGauge GaugeOf(string label) => Window
        .GetVisualDescendants()
        .OfType<HalfCircleGauge>()
        .Single(gauge => gauge.Label == label);

    [Fact]
    public Task The_delta_frequency_is_gone_while_the_inverter_is_not_synchronized() => HeadlessAvalonia.RunAsync(async () =>
    {
        var gen24System = Start(inverterFrequency: 0);
        await HeadlessAvalonia.SettleAsync();

        // The two readings themselves stay: that the inverter reports 0 Hz is worth seeing.
        Assert.True(GaugeOf(Resources.Grid).IsVisible);
        Assert.True(GaugeOf(Resources.Inverter).IsVisible);
        Assert.False(GaugeOf(Resources.ΔFrequency).IsVisible);

        // And it comes back as soon as the inverter synchronizes, without the page being shown again.
        gen24System.Sensors!.Inverter!.InverterFrequency = 49.98;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(GaugeOf(Resources.ΔFrequency).IsVisible);

        gen24System.Sensors.Inverter.InverterFrequency = 9.999;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(GaugeOf(Resources.ΔFrequency).IsVisible);
    });

    /// <summary>
    /// A reading that has not arrived yet is not a reading below 10 Hz. The gauge shows its own dashes for it,
    /// and the group does not jump about while the first update is on its way.
    /// </summary>
    [Fact]
    public Task A_frequency_that_was_never_reported_leaves_the_delta_where_it_is() => HeadlessAvalonia.RunAsync(async () =>
    {
        Start(inverterFrequency: null, gridFrequency: null);
        await HeadlessAvalonia.SettleAsync();

        Assert.True(GaugeOf(Resources.ΔFrequency).IsVisible);
    });
}
