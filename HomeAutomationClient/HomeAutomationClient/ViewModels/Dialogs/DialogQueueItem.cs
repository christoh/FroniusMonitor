namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// What the dialog frame of <c>MainView</c> shows: one dialog's body and the parameters that decide how the frame
/// around it behaves.
/// </summary>
/// <remarks>
/// The parameters are the live object of the dialog, not a copy of its fields, so a dialog can change its mind
/// while it is up - <see cref="DialogParameters.IsResizeable"/> follows the selected tab in the settings dialog.
/// <paramref name="Title"/> is read once, when the dialog is put up, and stays what it was then.
/// </remarks>
public record DialogQueueItem(
    string Title,
    object Body,
    DialogParameters Parameters,
    string? BusyText
);
