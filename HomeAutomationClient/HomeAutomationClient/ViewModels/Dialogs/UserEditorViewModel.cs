namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// Adds a user or edits one - the same dialog either way, the difference being whether a name can be typed and
/// whether a password has to be. Answers with the <see cref="UserAccount"/> to send, or null when cancelled;
/// sending it is the caller's job, so this dialog knows nothing about the server.
/// </summary>
public sealed partial class UserEditorViewModel : DialogBase<DialogParameters, UserAccount, UserEditorView>
{
    public UserEditorViewModel(DialogParameters parameters, UserInfo? existing) : base(parameters)
    {
        IsNew = existing == null;
        UserName = existing?.UserName ?? string.Empty;
        RoleItems = [.. Enum.GetValues<Roles>().Where(r => r != Roles.None && r != Roles.All).Select(r => new RoleItem(r) { IsSelected = existing?.Roles.HasFlag(r) ?? false })];
    }

    /// <summary>
    /// Adding, as opposed to editing. Only a new user has to be given a password; an existing one keeps the one
    /// they have unless a new one is typed. The name can be changed either way - the server accepts a rename,
    /// keyed by the name the user had when the dialog opened (see <c>IWebClientService.UpdateUser</c>).
    /// </summary>
    public bool IsNew { get; }

    public IReadOnlyList<RoleItem> RoleItems { get; }

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Loc), ErrorMessageResourceName = nameof(Loc.FieldRequired))]
    public partial string UserName { get; set; }

    [ObservableProperty, NotifyDataErrorInfo]
    [CustomValidation(typeof(UserEditorViewModel), nameof(ValidatePassword))]
    public partial string Password { get; set; } = string.Empty;

    public static ValidationResult? ValidatePassword(string? value, ValidationContext context)
    {
        var self = (UserEditorViewModel)context.ObjectInstance;
        return self.IsNew && string.IsNullOrEmpty(value) ? new ValidationResult(Loc.FieldRequired) : ValidationResult.Success;
    }

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

        Result = new UserAccount
        {
            UserName = UserName,
            Password = string.IsNullOrEmpty(Password) ? null : Password,
            Roles = RoleItems.Where(r => r.IsSelected).Aggregate(Roles.None, (roles, item) => roles | item.Role),
        };

        Close();
    });

    [RelayCommand]
    private Task Cancel() => AbortAsync();
}