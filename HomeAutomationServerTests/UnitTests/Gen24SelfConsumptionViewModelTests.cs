using System.Globalization;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The energy flow tab of the settings dialog: what the boxes, the sliders and the switches on it do to each
/// other, and what ends up going to the inverter.
/// </summary>
/// <remarks>
/// <para>
/// None of this is visible in the view. A box and the slider beside it are two views of one number and have to
/// stay in step without chasing each other in circles; the two state of charge limits may not cross; the charging
/// sources are one list standing for two flags of the inverter; and the powers change sign on the way out,
/// because to a battery the house is power flowing the other way. Every one of those is a rule somebody could
/// break while tidying the view model up.
/// </para>
/// <para>
/// The tab is built with an <see cref="ITabHost"/> of its own rather than the dialog view model, which is what
/// makes this possible without starting the client: see that interface.
/// </para>
/// </remarks>
public class Gen24SelfConsumptionViewModelTests
{
    /// <summary>Stands in for the dialog: a tab only ever asks it for somewhere to put its busy text and toast.</summary>
    private sealed class Host : ITabHost
    {
        public string? BusyText { get; set; }

        public string? ToastText { get; set; }
    }

    private static Gen24BatterySettings Loaded() => new()
    {
        Mode = OptimizationMode.Manual,
        RequestedGridPower = -2500,
        Limits = SocLimits.Override,
        SocMin = 10,
        SocMax = 90,
        ChargeFromAc = true,
        ChargeFromGrid = false,
        BatteryAcChargingMaxPower = -3000,
        BackupCriticalSoc = 7,
        BackupReserve = 20,
        IsEnabled = true,
        IsInCalibration = false,
        IsInServiceMode = false,
        IsAcCoupled = true,
    };

    private static Gen24SelfConsumptionViewModel Tab(params Gen24ChargingRule[] rules) =>
        new(new Host(), "test", Loaded(), rules);

    private static Gen24ChargingRule Rule(string start, string end, ChargingRuleType type = ChargingRuleType.MaximumCharge, int power = 5000, bool isActive = true) => new()
    {
        StartTime = start,
        EndTime = end,
        RuleType = type,
        Power = power,
        IsActive = isActive,
        Monday = true,
        Tuesday = true,
        Wednesday = true,
        Thursday = true,
        Friday = true,
        Saturday = true,
        Sunday = true,
    };

    [Fact]
    public void What_the_inverter_holds_reaches_the_boxes_and_the_sliders()
    {
        var tab = Tab();

        // The inverter keeps one signed number for feeding in and taking out; the tab shows the direction as a
        // switch and the amount as a positive number.
        Assert.True(tab.IsFeedIn);
        Assert.Equal("2500", tab.RequestedGridPowerText);
        Assert.Equal(Math.Log10(2500), tab.LogGridPower, 6);

        Assert.Equal("10", tab.SocMinText);
        Assert.Equal(10d, tab.SocMin);
        Assert.Equal("90", tab.SocMaxText);
        Assert.Equal(90d, tab.SocMax);

        // Negative in the inverter, positive on screen: to the battery, charging from the house flows the other
        // way.
        Assert.Equal("3000", tab.BatteryAcChargingMaxPowerText);

        Assert.Equal(Gen24ChargingSources.Home, tab.SelectedChargingSources);
        Assert.True(tab.IsManualMode);
        Assert.True(tab.ShowSocLimits);
        Assert.True(tab.ShowAcChargingPower);
        Assert.Empty(tab.Problems());
    }

    [Fact]
    public void A_battery_charged_from_the_grid_is_charged_from_the_house_as_well()
    {
        // The inverter does not always say so, and the combo box has no entry for the grid on its own, so both
        // entries would be wrong. FroniusMonitor fixes it up the same way.
        var settings = Loaded();
        settings.ChargeFromAc = false;
        settings.ChargeFromGrid = true;

        var tab = new Gen24SelfConsumptionViewModel(new Host(), "test", settings, []);

        Assert.Equal(Gen24ChargingSources.HomeAndGrid, tab.SelectedChargingSources);
        Assert.True(tab.Settings.ChargeFromAc);
    }

    [Theory]
    [InlineData(Gen24ChargingSources.InverterPv, false, false)]
    [InlineData(Gen24ChargingSources.Home, true, false)]
    [InlineData(Gen24ChargingSources.HomeAndGrid, true, true)]
    public void The_charging_sources_are_two_flags_of_the_inverter(Gen24ChargingSources selected, bool chargeFromAc, bool chargeFromGrid)
    {
        var tab = Tab();

        tab.SelectedChargingSources = selected;

        Assert.Equal(chargeFromAc, tab.Settings.ChargeFromAc);
        Assert.Equal(chargeFromGrid, tab.Settings.ChargeFromGrid);
        Assert.Equal(chargeFromAc, tab.ShowAcChargingPower);
    }

    [Fact]
    public void Pushing_the_lower_state_of_charge_limit_up_takes_the_upper_one_with_it()
    {
        var tab = Tab();

        tab.SocMin = 95;

        Assert.Equal(95d, tab.SocMax);
        Assert.Equal("95", tab.SocMinText);
        Assert.Equal("95", tab.SocMaxText);
    }

    [Fact]
    public void Pulling_the_upper_state_of_charge_limit_down_takes_the_lower_one_with_it()
    {
        var tab = Tab();

        tab.SocMax = 5;

        Assert.Equal(5d, tab.SocMin);
        Assert.Equal("5", tab.SocMaxText);
        Assert.Equal("5", tab.SocMinText);
    }

    [Fact]
    public void The_limits_are_left_alone_while_they_do_not_cross()
    {
        var tab = Tab();

        tab.SocMin = 20;
        tab.SocMax = 80;

        Assert.Equal(20d, tab.SocMin);
        Assert.Equal(80d, tab.SocMax);
    }

    [Fact]
    public void A_box_and_its_slider_stay_in_step()
    {
        var tab = Tab();

        tab.SocMinText = "42";
        Assert.Equal(42d, tab.SocMin);

        tab.SocMin = 55;
        Assert.Equal("55", tab.SocMinText);
    }

    [Fact]
    public void Text_the_rule_refuses_leaves_the_slider_where_it_was()
    {
        // A half typed number is not a position, and a slider jumping about while the box is being filled in
        // would be worse than one that waits. The box keeps what was typed, so it can be corrected.
        var tab = Tab();

        tab.SocMinText = "nonsense";

        Assert.Equal(10d, tab.SocMin);
        Assert.Equal("nonsense", tab.SocMinText);
        Assert.NotEmpty(tab.GetErrors(nameof(tab.SocMinText)));
        Assert.NotEmpty(tab.Problems());
    }

    [Fact]
    public void An_emptied_box_is_refused_rather_than_read_as_nothing()
    {
        // These are not optional the way a Modbus address is: the inverter has a number for every one of them
        // and an empty box would leave the delta saying "clear it".
        var tab = Tab();

        tab.BackupReserveText = string.Empty;

        Assert.NotEmpty(tab.GetErrors(nameof(tab.BackupReserveText)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(5000)]
    [InlineData(50000)]
    public void A_power_and_its_logarithmic_slider_stay_in_step(int watts)
    {
        var tab = Tab();

        tab.RequestedGridPowerText = watts.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(Math.Log10(watts), tab.LogGridPower, 6);

        tab.LogGridPower = Math.Log10(watts);
        Assert.Equal(watts.ToString(CultureInfo.InvariantCulture), tab.RequestedGridPowerText);
    }

    [Fact]
    public void A_power_of_nothing_puts_the_slider_at_the_end_of_its_travel()
    {
        // Zero has no logarithm and the slider has to sit somewhere.
        var settings = Loaded();
        settings.RequestedGridPower = 0;

        var tab = new Gen24SelfConsumptionViewModel(new Host(), "test", settings, []);

        Assert.Equal("0", tab.RequestedGridPowerText);
        Assert.Equal(-0.305d, tab.LogGridPower, 6);
    }

    [Fact]
    public void The_direction_switch_decides_the_sign_of_the_grid_power()
    {
        var tab = Tab();

        tab.RequestedGridPowerText = "2500";
        tab.IsFeedIn = false;
        tab.CopyToSettings();
        Assert.Equal(2500, tab.Settings.RequestedGridPower);

        tab.IsFeedIn = true;
        tab.CopyToSettings();
        Assert.Equal(-2500, tab.Settings.RequestedGridPower);
    }

    [Fact]
    public void Charging_from_the_house_goes_to_the_inverter_as_a_negative_power()
    {
        var tab = Tab();

        tab.BatteryAcChargingMaxPowerText = "4000";
        tab.CopyToSettings();

        Assert.Equal(-4000, tab.Settings.BatteryAcChargingMaxPower);
    }

    [Fact]
    public void Switching_the_danger_off_puts_back_what_the_inverter_holds()
    {
        var tab = Tab();
        tab.EnableDanger = true;

        tab.Settings.IsEnabled = false;
        tab.Settings.IsInCalibration = true;
        tab.Settings.IsInServiceMode = true;
        tab.Settings.IsAcCoupled = false;

        tab.EnableDanger = false;

        Assert.True(tab.Settings.IsEnabled);
        Assert.False(tab.Settings.IsInCalibration);
        Assert.False(tab.Settings.IsInServiceMode);
        Assert.True(tab.Settings.IsAcCoupled);
    }

    [Fact]
    public void Undo_puts_everything_back_whatever_the_boxes_hold()
    {
        var tab = Tab(Rule("01:00", "02:00"));
        tab.SocMinText = string.Empty;
        tab.RequestedGridPowerText = "nonsense";
        tab.BackupReserveText = "5";
        tab.SelectedChargingSources = Gen24ChargingSources.HomeAndGrid;
        tab.AddChargingRuleCommand.Execute(null);

        tab.UndoCommand.Execute(null);

        Assert.Equal("10", tab.SocMinText);
        Assert.Equal("2500", tab.RequestedGridPowerText);
        Assert.Equal("20", tab.BackupReserveText);
        Assert.Equal(Gen24ChargingSources.Home, tab.SelectedChargingSources);
        Assert.Single(tab.ChargingRules);
        Assert.Empty(tab.Problems());
    }

    [Fact]
    public void The_rules_arrive_in_the_order_they_run_in()
    {
        var tab = Tab(Rule("18:00", "20:00"), Rule("01:00", "02:00"), Rule("01:00", "01:30"));

        Assert.Equal(["01:00", "01:00", "18:00"], tab.ChargingRules.Select(rule => rule.StartTimeText));
        Assert.Equal(["01:30", "02:00", "20:00"], tab.ChargingRules.Select(rule => rule.EndTimeText));
    }

    [Fact]
    public void A_rule_that_ends_before_it_starts_is_refused()
    {
        var tab = Tab(Rule("20:00", "18:00"));

        var problems = tab.Problems();

        Assert.Single(problems);
        Assert.Contains("20:00", problems[0]);
    }

    [Fact]
    public void Two_rules_that_contradict_each_other_are_refused()
    {
        // Both active, overlapping in time, and the same kind of rule: the inverter cannot do both.
        var tab = Tab(Rule("01:00", "03:00"), Rule("02:00", "04:00"));

        Assert.Single(tab.Problems());
    }

    [Fact]
    public void Rules_that_do_not_overlap_are_left_alone()
    {
        var tab = Tab(Rule("01:00", "02:00"), Rule("02:00", "03:00"));

        Assert.Empty(tab.Problems());
    }

    [Fact]
    public void An_inactive_rule_cannot_contradict_anything()
    {
        var tab = Tab(Rule("01:00", "03:00"), Rule("02:00", "04:00", isActive: false));

        Assert.Empty(tab.Problems());
    }

    [Fact]
    public void A_time_the_inverter_could_not_read_is_refused()
    {
        var tab = Tab(Rule("01:00", "02:00"));

        tab.ChargingRules[0].EndTimeText = "24:30";

        Assert.NotEmpty(tab.Problems());
    }

    [Fact]
    public void A_rule_can_be_added_and_deleted()
    {
        var tab = Tab(Rule("01:00", "02:00"));

        tab.AddChargingRuleCommand.Execute(null);
        Assert.Equal(2, tab.ChargingRules.Count);

        // A new rule starts inactive, so it changes nothing until it has been filled in and switched on.
        Assert.False(tab.ChargingRules[1].Rule.IsActive);
        Assert.Empty(tab.Problems());

        tab.ChargingRules[1].DeleteCommand.Execute(null);
        Assert.Single(tab.ChargingRules);
    }

    [Fact]
    public void What_a_rule_holds_reaches_its_boxes_and_goes_back()
    {
        var tab = Tab(Rule("01:15", "02:45", ChargingRuleType.MinimumDischarge, 1234));
        var rule = tab.ChargingRules.Single();

        Assert.Equal("01:15", rule.StartTimeText);
        Assert.Equal("02:45", rule.EndTimeText);
        Assert.Equal("1234", rule.PowerText);
        Assert.Equal(ChargingRuleType.MinimumDischarge, rule.Rule.RuleType);

        rule.StartTimeText = "03:00";
        rule.PowerText = "4321";
        rule.CopyToRule();

        Assert.Equal("03:00", rule.Rule.StartTime);
        Assert.Equal(4321, rule.Rule.Power);
    }
}
