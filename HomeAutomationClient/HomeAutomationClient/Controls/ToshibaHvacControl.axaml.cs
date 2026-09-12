using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// One Toshiba air conditioner on the dashboard. The dashboard's data template sets <see cref="Device"/> and
/// <see cref="DeviceControlBase.DeviceKey"/>; this code behind passes both to a <see cref="ToshibaHvacViewModel"/>
/// of its own, which is the data context below the root and owns every button - see <c>ViewModelsForInteractionLogic</c>.
/// What stays here is the visual: the background that follows the power state, as in <see cref="PowerConsumer"/>.
/// </summary>
/// <remarks>
/// The view model is the data context of the inner <c>Root</c> element and not of the control, because the
/// control's own data context is the dashboard item the template binds <see cref="Device"/> from. Setting it on
/// the control would point that binding at the view model instead.
/// </remarks>
public partial class ToshibaHvacControl : DeviceControlBase
{
    public static readonly StyledProperty<ToshibaHvacMappingDevice?> DeviceProperty = AvaloniaProperty.Register<ToshibaHvacControl, ToshibaHvacMappingDevice?>(nameof(Device));

    public ToshibaHvacMappingDevice? Device
    {
        get => GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    private ToshibaHvacViewModel? ViewModel => Root.DataContext as ToshibaHvacViewModel;

    public ToshibaHvacControl()
    {
        InitializeComponent();
        Root.DataContext = IoC.TryGetRegistered<ToshibaHvacViewModel>();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.Property.Name)
        {
            case nameof(Device):
                if (e.OldValue is ToshibaHvacMappingDevice oldDevice)
                {
                    oldDevice.State.PropertyChanged -= OnStateChanged;
                }

                if (e.NewValue is ToshibaHvacMappingDevice newDevice)
                {
                    newDevice.State.PropertyChanged += OnStateChanged;
                }

                if (ViewModel != null)
                {
                    ViewModel.Device = Device;
                }

                ChangeOuter();
                break;

            case nameof(DeviceKey):
                if (ViewModel != null)
                {
                    ViewModel.DeviceKey = DeviceKey as string;
                }

                break;
        }
    }

    /// <summary>The state arrives on the hub's thread; only the background is touched here, and on the UI thread.</summary>
    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ToshibaHvacStateData.IsTurnedOn) or nameof(ToshibaHvacStateData.IsSelfCleaning) or nameof(ToshibaHvacStateData.StateData) or "" or null)
        {
            Dispatcher.UIThread.InvokeAsync(ChangeOuter);
        }
    }

    protected override void ChangeInner()
    {
        LcdProvider.Background = LcdBackground;
    }

    /// <summary>The WPF PowerStatus2Brush: cleaning, on, off - in the dashboard's colours for those states.</summary>
    protected override void ChangeOuter()
    {
        BackgroundProvider.Background = Device switch
        {
            null => OuterOther,
            { State.IsSelfCleaning: true } => CleaningBackground,
            { State.IsTurnedOn: true } => OuterRunning,
            _ => OuterOther,
        };
    }
}
