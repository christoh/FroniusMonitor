using De.Hochstaetter.Fronius.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public partial class InverterDetailsViewModel : ViewModelBase
{
    /// <summary>
    /// Typed access to one gauge group switch. Lets <see cref="ShowAll"/> and <see cref="ResetToDefaultCommand"/>
    /// read and write every group without reflection.
    /// </summary>
    private sealed record GroupSwitch(Func<InverterDetailsViewModel, bool> Get, Action<InverterDetailsViewModel, bool> Set);

    /// <summary>
    /// Every gauge group switch. A new gauge group needs its property here as well, otherwise
    /// <see cref="ShowAll"/> ignores it.
    /// </summary>
    private static readonly ImmutableArray<GroupSwitch> GroupSwitches =
    [
        new(viewModel => viewModel.DcPower, (viewModel, isOn) => viewModel.DcPower = isOn),
        new(viewModel => viewModel.DcVoltage, (viewModel, isOn) => viewModel.DcVoltage = isOn),
        new(viewModel => viewModel.DcCurrent, (viewModel, isOn) => viewModel.DcCurrent = isOn),
        new(viewModel => viewModel.Temperatures, (viewModel, isOn) => viewModel.Temperatures = isOn),
        new(viewModel => viewModel.Fans, (viewModel, isOn) => viewModel.Fans = isOn),
        new(viewModel => viewModel.AcPhaseVoltageFeedIn, (viewModel, isOn) => viewModel.AcPhaseVoltageFeedIn = isOn),
        new(viewModel => viewModel.AcPhaseVoltageInverter, (viewModel, isOn) => viewModel.AcPhaseVoltageInverter = isOn),
        new(viewModel => viewModel.DeltaAcPhaseVoltageFeedIn, (viewModel, isOn) => viewModel.DeltaAcPhaseVoltageFeedIn = isOn),
        new(viewModel => viewModel.AcLineVoltageFeedIn, (viewModel, isOn) => viewModel.AcLineVoltageFeedIn = isOn),
        new(viewModel => viewModel.AcLineVoltageInverter, (viewModel, isOn) => viewModel.AcLineVoltageInverter = isOn),
        new(viewModel => viewModel.DeltaAcLineVoltageFeedIn, (viewModel, isOn) => viewModel.DeltaAcLineVoltageFeedIn = isOn),
        new(viewModel => viewModel.Frequency, (viewModel, isOn) => viewModel.Frequency = isOn),
        new(viewModel => viewModel.ActivePower, (viewModel, isOn) => viewModel.ActivePower = isOn),
        new(viewModel => viewModel.ApparentPower, (viewModel, isOn) => viewModel.ApparentPower = isOn),
        new(viewModel => viewModel.ReactivePower, (viewModel, isOn) => viewModel.ReactivePower = isOn),
        new(viewModel => viewModel.PowerFactor, (viewModel, isOn) => viewModel.PowerFactor = isOn),
        new(viewModel => viewModel.Efficiency, (viewModel, isOn) => viewModel.Efficiency = isOn),
        new(viewModel => viewModel.AcCurrent, (viewModel, isOn) => viewModel.AcCurrent = isOn),
        new(viewModel => viewModel.OutOfBalance, (viewModel, isOn) => viewModel.OutOfBalance = isOn),
    ];

    private readonly MainViewModel mainViewModel;

    /// <summary>
    /// The default state of every gauge group, in the order of <see cref="GroupSwitches"/>. Taken from the property
    /// initializers below, so that <see cref="ResetToDefaultCommand"/> does not repeat them.
    /// </summary>
    private readonly ImmutableArray<bool> defaultGroupValues;

    public InverterDetailsViewModel(MainViewModel mainViewModel)
    {
        this.mainViewModel = mainViewModel;

        // The constructor body runs after all property initializers, so this captures exactly what they declared.
        defaultGroupValues = [..GroupSwitches.Select(groupSwitch => groupSwitch.Get(this))];
    }

    [ObservableProperty]
    public partial Gen24System Gen24System { get; set; } = null!;

    /// <summary>
    /// The gauge settings live on the singleton main view model, next to the switch in the main view.
    /// </summary>
    public MainViewModel MainViewModel => mainViewModel;

    /// <summary>
    /// Turns all gauge groups on or off, and is on itself while every single group is on. There is no
    /// indeterminate state, because the OnOff switch template has no visual for one.
    /// </summary>
    /// <remarks>
    /// Calculated instead of stored, so it can never disagree with the groups. Every group notifies for it
    /// (<see cref="NotifyPropertyChangedForAttribute"/>), and the setter batches its writes with
    /// <see cref="BindableBase.IsNotifying"/> so that the groups are notified once, not one by one.
    /// </remarks>
    public bool ShowAll
    {
        get => GroupSwitches.All(groupSwitch => groupSwitch.Get(this));

        set
        {
            IsNotifying = false;

            try
            {
                GroupSwitches.Apply(groupSwitch => groupSwitch.Set(this, value));
            }
            finally
            {
                Refresh(true);
            }
        }
    }

    #region Gauge groups

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool DcPower { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool DcVoltage { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool DcCurrent { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool Temperatures { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool Fans { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool AcPhaseVoltageFeedIn { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool AcPhaseVoltageInverter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool DeltaAcPhaseVoltageFeedIn { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool AcLineVoltageFeedIn { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool AcLineVoltageInverter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool DeltaAcLineVoltageFeedIn { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool Frequency { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool ActivePower { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool ApparentPower { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool ReactivePower { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool PowerFactor { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool Efficiency { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool AcCurrent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAll))]
    public partial bool OutOfBalance { get; set; }

    #endregion

    /// <summary>
    /// Puts every gauge group back to the default declared in its property initializer.
    /// </summary>
    [RelayCommand]
    private void ResetToDefault()
    {
        IsNotifying = false;

        try
        {
            for (var index = 0; index < GroupSwitches.Length; index++)
            {
                GroupSwitches[index].Set(this, defaultGroupValues[index]);
            }
        }
        finally
        {
            Refresh(true);
        }
    }
}
