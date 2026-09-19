using Avalonia.Controls.Primitives;
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

            // Both groups start out off, and a group that is off is not in the tree at all. These tests are
            // about gauges inside them, so they are switched on the way a user would.
            view.ViewModel.Frequency = true;
            view.ViewModel.PowerFactor = true;
            view.ViewModel.DeltaAcPhaseVoltageFeedIn = true;
            view.ViewModel.DeltaAcLineVoltageFeedIn = true;
        });
        return gen24System;
    }

    private static ChildWindow Window => HeadlessAvalonia.Windows.OfType<ChildWindow>().Single(window => window.Title == Title);

    /// <summary>
    /// The gauge by the label the user reads, so the test breaks if the page stops showing that one. Single, not
    /// First: the labels used here appear once across the groups these tests switch on, and a second one would
    /// mean the test is no longer looking at what it thinks it is.
    /// </summary>
    private static HalfCircleGauge GaugeOf(string label) => Gauges.Single(gauge => gauge.Label == label);

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
    /// The two groups that hold a difference between the inverter and the grid go with the ΔFrequency gauge:
    /// below 10 Hz the inverter is not synchronized, so what is in them is the grid's own voltage rather than a
    /// difference. Whole groups here, not single gauges, because every gauge in them is such a difference.
    /// </summary>
    [Fact]
    public Task The_delta_voltage_groups_go_while_the_inverter_is_not_synchronized() => HeadlessAvalonia.RunAsync(async () =>
    {
        var gen24System = Start(inverterFrequency: 0);
        await HeadlessAvalonia.SettleAsync();

        Assert.False(GroupOf(Resources.ΔAcPhaseVoltageFeedIn).IsVisible);
        Assert.False(GroupOf(Resources.ΔAcLineVoltageFeedIn).IsVisible);

        // The frequency group stays - only the one gauge inside it that is a difference goes away.
        Assert.True(GroupOf(Resources.Frequency).IsVisible);
        Assert.True(GaugeOf(Resources.Grid).IsVisible);

        gen24System.Sensors!.Inverter!.InverterFrequency = 50;
        await HeadlessAvalonia.SettleAsync();
        Assert.True(GroupOf(Resources.ΔAcPhaseVoltageFeedIn).IsVisible);
        Assert.True(GroupOf(Resources.ΔAcLineVoltageFeedIn).IsVisible);
    });

    /// <summary>The user's switch still has the last word, at any frequency.</summary>
    [Fact]
    public Task A_delta_voltage_group_the_user_switched_off_stays_off() => HeadlessAvalonia.RunAsync(async () =>
    {
        Start(inverterFrequency: 50);
        await HeadlessAvalonia.SettleAsync();
        Assert.True(GroupOf(Resources.ΔAcPhaseVoltageFeedIn).IsVisible);

        ViewModel.DeltaAcPhaseVoltageFeedIn = false;
        await HeadlessAvalonia.SettleAsync();
        Assert.False(GroupOf(Resources.ΔAcPhaseVoltageFeedIn).IsVisible);
    });

    /// <summary>
    /// The cos(phi) gauges read the amount and leave the sign to the read-out; see <see cref="GaugeDialTests"/>
    /// for what that does to the needle. Here it is the style of the group that is under test: all four gauges,
    /// and none of the others on the page.
    /// </summary>
    [Fact]
    public Task Only_the_cos_phi_gauges_read_the_amount_instead_of_the_value() => HeadlessAvalonia.RunAsync(async () =>
    {
        Start(inverterFrequency: 50);
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(4, Gauges.Count(gauge => gauge.DialShowsAbsoluteValue));
        Assert.All(new[] { "L1", "L2", "L3", Resources.Total }, label => Assert.Contains(label, CosPhiLabels));
        Assert.False(GaugeOf(Resources.ΔFrequency).DialShowsAbsoluteValue);

        // And their scale runs 0 to 1, because with the sign gone there is no lower half to show.
        Assert.All(Gauges.Where(gauge => gauge.DialShowsAbsoluteValue), gauge =>
        {
            Assert.Equal(0, gauge.Minimum);
            Assert.Equal(1, gauge.Maximum);
        });
    });

    private static IReadOnlyList<HalfCircleGauge> Gauges => Window.GetVisualDescendants().OfType<HalfCircleGauge>().ToList();

    private static InverterDetailsViewModel ViewModel => Assert.IsType<InverterDetailsView>(Window.HostedContent).ViewModel;

    /// <summary>A gauge group by its header, which is the caption the user reads above it.</summary>
    private static HeaderedContentControl GroupOf(string header) => Window
        .GetVisualDescendants()
        .OfType<HeaderedContentControl>()
        .Single(group => Equals(group.Header, header));

    private static IReadOnlyList<string?> CosPhiLabels => Gauges.Where(gauge => gauge.DialShowsAbsoluteValue).Select(gauge => gauge.Label).ToList();

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
