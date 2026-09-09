using System.ComponentModel;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// One string tracker of the inverter settings tab: what it may do with the power of its strings, how much of it
/// there is, and - where it is set to a fixed working point - at what voltage.
/// </summary>
/// <remarks>
/// <para>
/// An inverter has one or two of these and both are edited the same way, so there is one view model and one view
/// for a tracker rather than two of each. The difference between them is only in the names: every value of a
/// tracker is its own channel of the inverter, <c>PV_MODE_MPP_01_U16</c> against <c>PV_MODE_MPP_02_U16</c>, and
/// the captions are those channels' own words. <see cref="Index"/> is what picks them.
/// </para>
/// <para>
/// The two boxes here are numbers, so they are <see cref="string"/> properties with the rule on them and the
/// sliders beside them are a second view of the same value - the text input rule of
/// <c>.claude/rules/ViewModelsForInteractionLogic.md</c>, and the same shape as
/// <see cref="Gen24SelfConsumptionViewModel"/>.
/// </para>
/// </remarks>
public sealed partial class Gen24MpptViewModel : ViewModelBase
{
    /// <summary>
    /// Where the peak power slider starts and ends, logarithmically: the bottom is below one watt, which is how a
    /// tracker with nothing on it sits at the far left, and the top is the 2 MW the inverter accepts.
    /// </summary>
    private const double LogWattPeakMinimum = -0.30980391997148634d;

    /// <inheritdoc cref="LogWattPeakMinimum"/>
    private const double LogWattPeakMaximum = 5.3010299956639812d;

    private readonly Gen24InverterSettingsViewModel owner;
    private bool isSyncing;

    public Gen24MpptViewModel(Gen24InverterSettingsViewModel owner, Gen24MpptBase tracker, int index)
    {
        this.owner = owner;
        Tracker = tracker;
        Index = index;

        // The combo boxes write the tracker directly - they cannot produce a value it would refuse - so this is
        // where a change of the power mode is noticed, and what it decides is which of the two fields below is
        // there at all. Nothing detaches: a new view model is built for a new tracker whenever the tab resets.
        Tracker.PropertyChanged += OnTrackerPropertyChanged;

        CopyFromTracker();
    }

    /// <summary>The tracker itself, which is the object that goes to the inverter.</summary>
    public Gen24MpptBase Tracker { get; }

    /// <summary>1 or 2. It picks the channel names, and there is nothing else different about the two.</summary>
    public int Index { get; }

    public double WattPeakSliderMinimum => LogWattPeakMinimum;

    public double WattPeakSliderMaximum => LogWattPeakMaximum;

    public double FixedVoltageMinimum => 80d;

    public double FixedVoltageMaximum => 800d;

    #region What the inverter calls all of this

    /// <summary>
    /// The captions, in the inverter's own words. They are here and not in the view - which is where the other
    /// tabs keep theirs, through the localization markup extensions - because the key of every one of them
    /// depends on which tracker this is, and a markup extension takes a constant.
    /// </summary>
    /// <remarks>
    /// The two that stand beside a box carry the colon that separates them from it, the way the WPF view put it
    /// in a <c>Run</c> of its own. The two that label a combo box below them do not.
    /// </remarks>
    public string Header => Channel($"MPPT{Index}");

    /// <inheritdoc cref="Header"/>
    public string PowerModeCaption => Channel($"PV_MODE_MPP_0{Index}_U16");

    /// <inheritdoc cref="Header"/>
    public string DynamicPeakManagerCaption => Channel($"PV_MODE_DYNAMICPEAK_0{Index}_U16");

    /// <inheritdoc cref="Header"/>
    public string WattPeakCaption => $"{Channel($"PV_POWERACTIVE_CONNECTED_PEAK_MAX_0{Index}_U32")}:";

    /// <inheritdoc cref="Header"/>
    public string FixedVoltageCaption => $"{Channel($"PV_VOLTAGE_FIX_0{Index}_F32")}:";

    #endregion

    public IReadOnlyList<ListItemModel<MpptPowerMode>> PowerModes => owner.PowerModes;

    public IReadOnlyList<ListItemModel<MpptOnOff>> DynamicPeakManagerModes => owner.DynamicPeakManagerModes;

    /// <summary>How much peak power is connected to this tracker, as the box holds it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxInt(0, 2_000_000, AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.Power))]
    public partial string? WattPeakText { get; set; }

    /// <summary>
    /// The same number on a slider, logarithmically: the interesting settings are all at the bottom of a range
    /// that goes to 2 MW.
    /// </summary>
    [ObservableProperty]
    public partial double LogWattPeak { get; set; }

    /// <summary>The working point of a tracker that is not looking for one, as the box holds it.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinMaxDouble(80, 800, AllowEmpty = false, PropertyDisplayNameResourceKey = nameof(Loc.Voltage))]
    public partial string? DcFixedVoltageText { get; set; }

    /// <inheritdoc cref="DcFixedVoltageText"/>
    [ObservableProperty]
    public partial double DcFixedVoltage { get; set; }

    /// <summary>Only a tracker that finds its own working point has a peak manager to set.</summary>
    public bool ShowDynamicPeakManager => Tracker.PowerMode is MpptPowerMode.Auto;

    /// <summary>And only one that does not needs a voltage.</summary>
    public bool ShowFixedVoltage => Tracker.PowerMode is MpptPowerMode.Fix;

    /// <summary>The tracker into the boxes and onto the sliders, both halves of every pair.</summary>
    public void CopyFromTracker()
    {
        var wattPeak = (long)(Tracker.WattPeak ?? 0);

        Guard(() =>
        {
            WattPeakText = NumericText.Of(wattPeak);
            LogWattPeak = Log(wattPeak);
        });

        var voltage = Tracker.DcFixedVoltage ?? FixedVoltageMinimum;

        Guard(() =>
        {
            DcFixedVoltageText = NumericText.Of(voltage, "F0");
            DcFixedVoltage = Math.Clamp(voltage, FixedVoltageMinimum, FixedVoltageMaximum);
        });

        NotifyVisibilities();
        ValidateAllProperties();
    }

    /// <summary>
    /// And back again, on the way to the inverter. Only called once the boxes have passed their rules. The two
    /// modes are not here: their combo boxes write the tracker as the user picks them.
    /// </summary>
    public void CopyToTracker()
    {
        Tracker.WattPeak = (uint?)NumericText.ToInteger(WattPeakText);
        Tracker.DcFixedVoltage = NumericText.ToNumber(DcFixedVoltageText);
    }

    public override string ToString() => Header;

    private string Channel(string key) => owner.Channel(key);

    private void NotifyVisibilities()
    {
        OnPropertyChanged(nameof(ShowDynamicPeakManager));
        OnPropertyChanged(nameof(ShowFixedVoltage));
    }

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.Log"/>
    private static double Log(long watts) => watts <= 0 ? LogWattPeakMinimum : Math.Clamp(Math.Log10(watts), LogWattPeakMinimum, LogWattPeakMaximum);

    private static long FromLog(double log) => (long)Math.Round(Math.Pow(10, log), MidpointRounding.AwayFromZero);

    /// <inheritdoc cref="Gen24SelfConsumptionViewModel.Guard"/>
    private void Guard(Action write)
    {
        if (isSyncing)
        {
            return;
        }

        isSyncing = true;

        try
        {
            write();
        }
        finally
        {
            isSyncing = false;
        }
    }

    /// <summary>
    /// The power mode decides which of the two fields below is there at all, so a change of it has to be passed
    /// on to the view. Any change here is also a change of the tab, which is what clears the toast.
    /// </summary>
    private void OnTrackerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Gen24MpptBase.PowerMode))
        {
            NotifyVisibilities();
        }

        owner.OnTrackerChanged();
    }

    partial void OnWattPeakTextChanged(string? value) => Guard(() =>
    {
        if (!GetErrors(nameof(WattPeakText)).Any() && NumericText.TryParseInteger(value, out var watts))
        {
            LogWattPeak = Log(watts);
        }
    });

    partial void OnLogWattPeakChanged(double value) => Guard(() => WattPeakText = NumericText.Of(FromLog(value)));

    partial void OnDcFixedVoltageTextChanged(string? value) => Guard(() =>
    {
        if (!GetErrors(nameof(DcFixedVoltageText)).Any() && NumericText.TryParseNumber(value, out var volts))
        {
            DcFixedVoltage = Math.Clamp(volts, FixedVoltageMinimum, FixedVoltageMaximum);
        }
    });

    partial void OnDcFixedVoltageChanged(double value) => Guard(() => DcFixedVoltageText = NumericText.Of(Math.Round(value), "F0"));
}
