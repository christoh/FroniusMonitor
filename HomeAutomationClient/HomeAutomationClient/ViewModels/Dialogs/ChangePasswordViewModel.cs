namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// Lets the logged in user change their own password. The same shape as <see cref="UserEditorViewModel"/> -
/// collect the input, validate it, hand back a request or null when cancelled - but for a password alone: there
/// is no name or role to change here, and the current password has to be typed to prove who is asking. There is
/// no confirmation field: the <see cref="Controls.PasswordBox"/> can reveal what was typed, which makes typing
/// the new password twice pointless.
/// </summary>
public sealed partial class ChangePasswordViewModel : DialogBase<DialogParameters, ChangePasswordRequest, ChangePasswordView>
{
    public ChangePasswordViewModel(DialogParameters parameters) : base(parameters)
    {
    }

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Loc), ErrorMessageResourceName = nameof(Loc.FieldRequired))]
    public partial string CurrentPassword { get; set; } = string.Empty;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Loc), ErrorMessageResourceName = nameof(Loc.FieldRequired))]
    public partial string NewPassword { get; set; } = string.Empty;

    public override Task AbortAsync()
    {
        Result = null;
        Close();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Ok() => TaskExceptionHandler(async () =>
    {
        ValidateAllProperties();

        if (HasErrors)
        {
            await new MessageBox
            {
                Text = $"{Loc.PleaseCorrectErrors}:",
                ItemList = [.. GetErrors().Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m!).Distinct()],
                Title = Loc.Error,
                Buttons = [Loc.Ok],
                Icon = new ErrorIcon(),
            }.Show().ConfigureAwait(true);

            return;
        }

        Result = new ChangePasswordRequest
        {
            CurrentPassword = CurrentPassword,
            NewPassword = NewPassword,
        };

        Close();
    });

    [RelayCommand]
    private Task Cancel() => AbortAsync();
}
