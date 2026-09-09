using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The inverter settings tab of the settings dialog: what the boxes, the sliders and the switches on it do to each
/// other, and what ends up in the settings that go to the inverter.
/// </summary>
/// <remarks>
/// <para>
/// The rules worth holding on to are the ones no field can see for itself. The three export limits are one order
/// - the soft limit at or below the hard one, the hard one at or below the peak power they are read against - and
/// moving any of them has to take the others with it. A limit that is switched off has to leave as a zero. The
/// power mode of a tracker decides which of its two remaining fields exists. And the danger switch has to put
/// back what it was opened for when it is closed again.
/// </para>
/// <para>
/// None of that is visible in the view, and the tab is built with an <see cref="ITabHost"/> of its own rather
/// than the dialog view model, so all of it can be asked without starting the client.
/// </para>
/// </remarks>
public class Gen24InverterSettingsViewModelTests
{
    /// <inheritdoc cref="Gen24SelfConsumptionViewModelTests"/>
    private sealed class Host : ITabHost
    {
        public string? BusyText { get; set; }

        public string? ToastText { get; set; }
    }

    private static Gen24InverterSettings Loaded() => new()
    {
        SystemName = "Hochstaetter",
        TimeZoneName = "Europe/Zurich",
        TimeSync = true,
        Mppt = new Gen24Mppt
        {
            Mppt1 = new Gen24Mppt1 { PowerMode = MpptPowerMode.Auto, DynamicPeakManager = MpptOnOff.On, WattPeak = 5000, DcFixedVoltage = 600 },
            Mppt2 = new Gen24Mppt2 { PowerMode = MpptPowerMode.Fix, DynamicPeakManager = MpptOnOff.Off, WattPeak = 3000, DcFixedVoltage = 400 },
        },
        PowerLimitSettings = new Gen24PowerLimitSettings
        {
            Visualization = new Gen24PowerLimitsVisualization { WattPeakReferenceValue = 8000 },
            ExportLimits = new Gen24PowerLimits
            {
                ActivePower = new Gen24PowerLimit
                {
                    IsEnabled = true,
                    PhaseMode = PhaseMode.PhaseSum,
                    HardLimit = new Gen24PowerLimitDefinition { IsEnabled = true, PowerLimit = 7000 },
                    SoftLimit = new Gen24PowerLimitDefinition { IsEnabled = true, PowerLimit = 4000 },
                },
            },
        },
    };

    private static Gen24InverterSettingsViewModel Tab(bool isTechnician = true, Gen24InverterSettings? settings = null) =>
        new(new Host(), "test", settings ?? Loaded(), isTechnician);

    #region What the tab shows when it opens

    [Fact]
    public void FillsTheBoxesAndTheSlidersFromTheSettings()
    {
        var tab = Tab();

        Assert.Equal("8000", tab.WattPeakReferenceText);
        Assert.Equal(8000, tab.WattPeakReference);
        Assert.Equal("7000", tab.HardLimitText);
        Assert.Equal(7000, tab.HardLimit);
        Assert.Equal("4000", tab.SoftLimitText);
        Assert.Equal(4000, tab.SoftLimit);

        Assert.Equal("5000", tab.Mppt1?.WattPeakText);
        Assert.Equal("3000", tab.Mppt2?.WattPeakText);
        Assert.Equal("600", tab.Mppt1?.DcFixedVoltageText);
    }

    [Fact]
    public void WorksOnACopySoThatUndoHasSomethingToGoBackTo()
    {
        var loaded = Loaded();
        var tab = Tab(settings: loaded);

        tab.Settings.SystemName = "Something else";

        Assert.Equal("Hochstaetter", loaded.SystemName);
        Assert.NotSame(loaded, tab.Settings);
        Assert.NotSame(loaded.Mppt, tab.Settings.Mppt);
        Assert.NotSame(loaded.PowerLimitSettings, tab.Settings.PowerLimitSettings);
    }

    [Fact]
    public void PutsTheTrackersBackAsTheyWereOnUndo()
    {
        var tab = Tab();

        tab.Settings.SystemName = "Something else";
        tab.Mppt1!.WattPeakText = "1234";
        tab.HardLimitText = "1500";
        tab.UndoCommand.Execute(null);

        Assert.Equal("Hochstaetter", tab.Settings.SystemName);
        Assert.Equal("5000", tab.Mppt1!.WattPeakText);
        Assert.Equal("7000", tab.HardLimitText);
        Assert.Equal(7000, tab.HardLimit);
    }

    [Fact]
    public void HasNoTrackerWhereTheInverterReportsNone()
    {
        var settings = Loaded();
        settings.Mppt = null;

        var tab = Tab(settings: settings);

        Assert.Null(tab.Mppt1);
        Assert.Null(tab.Mppt2);
        Assert.False(tab.ShowMppt1);
        Assert.False(tab.ShowMppt2);
    }

    [Fact]
    public void HasOneTrackerWhereTheInverterReportsOne()
    {
        var settings = Loaded();
        settings.Mppt!.Mppt2 = null;

        var tab = Tab(settings: settings);

        Assert.NotNull(tab.Mppt1);
        Assert.Null(tab.Mppt2);
        Assert.True(tab.ShowMppt1);
        Assert.False(tab.ShowMppt2);
    }

    /// <summary>
    /// What the inverter only lets a technician change is not offered to anybody else. Being refused by the
    /// inverter after typing it in is a worse way to find out.
    /// </summary>
    [Fact]
    public void HidesWhatOnlyATechnicianMayChange()
    {
        var tab = Tab(isTechnician: false);

        Assert.False(tab.ShowMppt1);
        Assert.False(tab.ShowMppt2);
        Assert.False(tab.ShowExportLimits);

        // And the settings themselves are still there, so nothing further down depends on the gate.
        Assert.NotNull(tab.Mppt1);
        Assert.NotNull(tab.Settings.Mppt);
    }

    #endregion

    #region A box and the slider beside it

    [Fact]
    public void MovesTheSliderWhenTheBoxIsTyped()
    {
        var tab = Tab();

        tab.HardLimitText = "5500";

        Assert.Equal(5500, tab.HardLimit);
    }

    [Fact]
    public void WritesTheBoxWhenTheSliderMoves()
    {
        var tab = Tab();

        tab.HardLimit = 6200;

        Assert.Equal("6200", tab.HardLimitText);
    }

    /// <summary>
    /// Text the rule refuses leaves the slider where it was: a half typed number is not a position, and a slider
    /// that jumped about while a box was being filled in would be worse than one that waits.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("999999")]
    public void LeavesTheSliderAloneWhereTheBoxIsRefused(string text)
    {
        var tab = Tab();

        tab.HardLimitText = text;

        Assert.Equal(7000, tab.HardLimit);
        Assert.True(tab.HasErrors);
    }

    /// <summary>The peak power of a tracker is on a logarithmic slider, so the round trip has to survive it.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("5000")]
    [InlineData("12000")]
    [InlineData("2000000")]
    public void KeepsThePeakPowerOfATrackerOnItsLogarithmicSlider(string watts)
    {
        var tab = Tab();

        tab.Mppt1!.WattPeakText = watts;
        var log = tab.Mppt1.LogWattPeak;

        // Whatever the slider now holds has to mean the same number.
        tab.Mppt1.LogWattPeak = log;

        Assert.Equal(watts, tab.Mppt1.WattPeakText);
    }

    #endregion

    #region The three limits are one order

    [Fact]
    public void PushingTheSoftLimitAboveTheHardOneTakesTheHardOneWithIt()
    {
        var tab = Tab();

        tab.SoftLimit = 7500;

        Assert.Equal(7500, tab.HardLimit);
        Assert.Equal("7500", tab.HardLimitText);
    }

    [Fact]
    public void PullingTheHardLimitBelowTheSoftOneTakesTheSoftOneWithIt()
    {
        var tab = Tab();

        tab.HardLimit = 2500;

        Assert.Equal(2500, tab.SoftLimit);
        Assert.Equal("2500", tab.SoftLimitText);
    }

    [Fact]
    public void RaisingALimitAboveThePeakPowerRaisesThePeakPower()
    {
        var tab = Tab();

        tab.HardLimit = 9000;

        Assert.Equal(9000, tab.WattPeakReference);
        Assert.Equal("9000", tab.WattPeakReferenceText);
    }

    [Fact]
    public void LoweringThePeakPowerBelowTheLimitsLowersBoth()
    {
        var tab = Tab();

        tab.WattPeakReference = 3000;

        Assert.Equal(3000, tab.HardLimit);
        Assert.Equal(3000, tab.SoftLimit);
        Assert.Equal("3000", tab.HardLimitText);
        Assert.Equal("3000", tab.SoftLimitText);
    }

    [Fact]
    public void TheOrderHoldsWhenTheBoxesAreTypedRatherThanTheSlidersMoved()
    {
        var tab = Tab();

        tab.SoftLimitText = "7800";

        Assert.Equal(7800, tab.HardLimit);
        Assert.Equal("7800", tab.HardLimitText);

        // The peak power was already above both, so it stays where it is: a limit only pushes what is in its way.
        Assert.Equal(8000, tab.WattPeakReference);
    }

    /// <inheritdoc cref="TheOrderHoldsWhenTheBoxesAreTypedRatherThanTheSlidersMoved"/>
    [Fact]
    public void TypingALimitAboveThePeakPowerRaisesThePeakPower()
    {
        var tab = Tab();

        tab.SoftLimitText = "9500";

        Assert.Equal(9500, tab.HardLimit);
        Assert.Equal(9500, tab.WattPeakReference);
        Assert.Equal("9500", tab.WattPeakReferenceText);
    }

    #endregion

    #region What goes to the inverter

    [Fact]
    public void CopiesTheBoxesIntoTheSettingsOnTheWayOut()
    {
        var tab = Tab();

        tab.Mppt1!.WattPeakText = "6400";
        tab.Mppt2!.DcFixedVoltageText = "450";
        tab.HardLimitText = "6800";
        tab.CopyToSettings();

        Assert.Equal(6400u, tab.Settings.Mppt?.Mppt1?.WattPeak);
        Assert.Equal(450, tab.Settings.Mppt?.Mppt2?.DcFixedVoltage);
        Assert.Equal(6800, tab.Settings.PowerLimitSettings.ExportLimits.ActivePower.HardLimit.PowerLimit);
    }

    /// <summary>
    /// A limit the user switched off leaves as a zero. The inverter keeps the number and the switch separately,
    /// and a disabled limit that still carries watts is a limit waiting to come back on with an old value.
    /// </summary>
    [Fact]
    public void SendsADisabledLimitAsZero()
    {
        var tab = Tab();
        var limits = tab.Settings.PowerLimitSettings.ExportLimits.ActivePower;

        limits.SoftLimit.IsEnabled = false;
        tab.CopyToSettings();

        Assert.Equal(0, limits.SoftLimit.PowerLimit);
        Assert.Equal(7000, limits.HardLimit.PowerLimit);
    }

    [Fact]
    public void SwitchingLimitingOffSwitchesBothLimitsOffAndZeroesThem()
    {
        var tab = Tab();
        var limits = tab.Settings.PowerLimitSettings.ExportLimits.ActivePower;

        limits.IsEnabled = false;
        tab.CopyToSettings();

        Assert.False(limits.HardLimit.IsEnabled);
        Assert.False(limits.SoftLimit.IsEnabled);
        Assert.Equal(0, limits.HardLimit.PowerLimit);
        Assert.Equal(0, limits.SoftLimit.PowerLimit);
    }

    /// <summary>
    /// Unlike the WPF dialog, which replaces the whole object and loses it. The peak power is what the inverter's
    /// display reads a limit against, not a limit itself, so switching limiting off is no reason to forget how
    /// big the system is.
    /// </summary>
    [Fact]
    public void SwitchingLimitingOffKeepsThePeakPowerOfTheSystem()
    {
        var tab = Tab();

        tab.Settings.PowerLimitSettings.ExportLimits.ActivePower.IsEnabled = false;
        tab.CopyToSettings();

        Assert.Equal(8000, tab.Settings.PowerLimitSettings.Visualization.WattPeakReferenceValue);
    }

    [Fact]
    public void SaysNothingHasChangedWhereNothingHas()
    {
        var tab = Tab();
        tab.CopyToSettings();

        var json = IoC.Get<IGen24JsonService>();

        Assert.False(json.GetUpdateToken(tab.Settings, Loaded()).HasAnyValue());
        Assert.False(tab.Settings.Mppt!.GetToken(Loaded().Mppt).HasAnyValue());
        Assert.False(tab.Settings.PowerLimitSettings.GetToken(Loaded().PowerLimitSettings).HasAnyValue());
    }

    /// <summary>
    /// Each of the three groups has an endpoint of its own, and a change in one of them must not make the tab
    /// write the other two.
    /// </summary>
    [Fact]
    public void ChangesOneGroupWithoutTouchingTheOthers()
    {
        var tab = Tab();
        tab.Settings.SystemName = "Something else";
        tab.CopyToSettings();

        var json = IoC.Get<IGen24JsonService>();

        Assert.True(json.GetUpdateToken(tab.Settings, Loaded()).HasAnyValue());
        Assert.False(tab.Settings.Mppt!.GetToken(Loaded().Mppt).HasAnyValue());
        Assert.False(tab.Settings.PowerLimitSettings.GetToken(Loaded().PowerLimitSettings).HasAnyValue());
    }

    [Fact]
    public void ChangingATrackerWritesOnlyTheTrackers()
    {
        var tab = Tab();
        tab.Mppt2!.WattPeakText = "3500";
        tab.CopyToSettings();

        var json = IoC.Get<IGen24JsonService>();

        Assert.False(json.GetUpdateToken(tab.Settings, Loaded()).HasAnyValue());
        Assert.True(tab.Settings.Mppt!.GetToken(Loaded().Mppt).HasAnyValue());
        Assert.False(tab.Settings.PowerLimitSettings.GetToken(Loaded().PowerLimitSettings).HasAnyValue());
    }

    #endregion

    #region What a tracker shows

    [Theory]
    [InlineData(MpptPowerMode.Auto, true, false)]
    [InlineData(MpptPowerMode.Fix, false, true)]
    [InlineData(MpptPowerMode.Off, false, false)]
    public void ShowsTheFieldThatBelongsToThePowerMode(MpptPowerMode mode, bool showsPeakManager, bool showsVoltage)
    {
        var tab = Tab();

        tab.Mppt1!.Tracker.PowerMode = mode;

        Assert.Equal(showsPeakManager, tab.Mppt1.ShowDynamicPeakManager);
        Assert.Equal(showsVoltage, tab.Mppt1.ShowFixedVoltage);
    }

    [Fact]
    public void EachTrackerNamesItsOwnChannels()
    {
        var tab = Tab();

        // Without an inverter to ask, the localization service answers with the key itself, which is exactly
        // what makes this worth asserting: the numbers in the keys are what tells the two trackers apart.
        Assert.Contains("01", tab.Mppt1!.WattPeakCaption, StringComparison.Ordinal);
        Assert.Contains("02", tab.Mppt2!.WattPeakCaption, StringComparison.Ordinal);
        Assert.Contains("01", tab.Mppt1.PowerModeCaption, StringComparison.Ordinal);
        Assert.Contains("02", tab.Mppt2.PowerModeCaption, StringComparison.Ordinal);
    }

    #endregion

    #region The danger switch

    [Fact]
    public void PutsWhatItGuardsBackWhenItIsSwitchedOffAgain()
    {
        var tab = Tab();

        tab.EnableDanger = true;
        tab.Settings.TimeZoneName = "Pacific/Auckland";
        tab.Settings.TimeSync = false;
        tab.EnableDanger = false;

        Assert.Equal("Europe/Zurich", tab.Settings.TimeZoneName);
        Assert.True(tab.Settings.TimeSync);
    }

    [Fact]
    public void LeavesWhatItDoesNotGuardAlone()
    {
        var tab = Tab();

        tab.EnableDanger = true;
        tab.Settings.SystemName = "Something else";
        tab.HardLimitText = "1200";
        tab.EnableDanger = false;

        Assert.Equal("Something else", tab.Settings.SystemName);
        Assert.Equal("1200", tab.HardLimitText);
    }

    #endregion

    #region What the user has to put right

    [Fact]
    public void HasNothingToComplainAboutWhenEverythingIsInRange()
    {
        Assert.Empty(Tab().Problems());
    }

    [Fact]
    public void ComplainsAboutABoxOfItsOwn()
    {
        var tab = Tab();

        tab.HardLimitText = "not a number";

        Assert.NotEmpty(tab.Problems());
    }

    /// <summary>A tracker is its own view model, so its errors have to be collected as well as the tab's own.</summary>
    [Fact]
    public void ComplainsAboutABoxOfATracker()
    {
        var tab = Tab();

        tab.Mppt2!.WattPeakText = string.Empty;

        Assert.NotEmpty(tab.Problems());
    }

    #endregion
}
