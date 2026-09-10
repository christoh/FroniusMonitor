using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The Wattpilot settings dialog: the frame around <see cref="WattPilotSettingsViewModel"/>, which holds every tab.
/// </summary>
/// <remarks>
/// Unlike the inverter dialog, whose tabs each write their own group, the whole of this dialog is one Apply - the
/// charger takes settings one key at a time and there is no group to speak of - so Apply, Undo and the danger
/// switch sit in the button row of the dialog beside Cancel, the way the WPF window has them, and the tabs hold
/// only fields. Nothing is read when the dialog opens: the client already holds the live device, kept current by
/// the deltas the server pushes, and the settings view model starts from a copy of it.
/// </remarks>
public sealed partial class WattPilotSettingsDialogViewModel : DialogBase<DialogParameters, bool, WattPilotSettingsDialogView>, ITabHost
{
    public WattPilotSettingsDialogViewModel(DialogParameters parameters, string deviceId, WattPilot wattPilot) : base(parameters)
    {
        Settings = new WattPilotSettingsViewModel(this, deviceId, wattPilot);
    }

    public WattPilotSettingsViewModel Settings { get; }

    /// <summary>What the toast in the button row says. The settings write to it through their <see cref="ITabHost"/>.</summary>
    [ObservableProperty]
    public partial string? ToastText { get; set; }

    [RelayCommand]
    private void CloseDialog()
    {
        Result = true;
        Close();
    }

    /// <summary>The close box of the frame. Nothing to unwind: the charger is only written on Apply.</summary>
    public override Task AbortAsync()
    {
        CloseDialog();
        return Task.CompletedTask;
    }
}
