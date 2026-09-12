using System.Collections.ObjectModel;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The list of users with add, edit and delete. Every change goes to the server at once and the list is read
/// back afterwards, so what is shown is always what the server holds.
/// </summary>
public sealed partial class UserManagementViewModel(DialogParameters parameters) : DialogBase<DialogParameters, bool, UserManagementView>(parameters)
{
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly MainViewModel mainViewModel = IoC.GetRegistered<MainViewModel>();
    private bool isInitialized;

    public ObservableCollection<UserInfo> Users { get; } = [];

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(EditCommand), nameof(DeleteCommand))]
    public partial UserInfo? SelectedUser { get; set; }

    private bool HasSelectedUser => SelectedUser != null;

    public override Task Initialize() => TaskExceptionHandler(async () =>
    {
        // Initialize runs again whenever a dialog on top of this one closes; the list is reloaded by the
        // command that opened that dialog, so a second load here would only flicker.
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        await base.Initialize().ConfigureAwait(true);
        await LoadUsers().ConfigureAwait(true);
    });

    public override Task AbortAsync()
    {
        Result = true;
        Close();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task CloseDialog() => AbortAsync();

    [RelayCommand]
    private Task Add() => TaskExceptionHandler(async () =>
    {
        var editor = new UserEditorViewModel(new DialogParameters { Title = Loc.AddUser }, null);

        if (await editor.ShowDialogAsync().ConfigureAwait(true) is not { } account)
        {
            return;
        }

        BusyText = Loc.AddUser;
        var result = await webClient.AddUser(account).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK)
        {
            await ShowHttpError(result).ConfigureAwait(true);
        }

        await LoadUsers().ConfigureAwait(true);
    });

    [RelayCommand(CanExecute = nameof(HasSelectedUser))]
    private Task Edit() => TaskExceptionHandler(async () =>
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        var editor = new UserEditorViewModel(new DialogParameters { Title = $"{Loc.EditUser}: {user.UserName}" }, user);

        if (await editor.ShowDialogAsync().ConfigureAwait(true) is not { } account)
        {
            return;
        }

        BusyText = Loc.EditUser;
        var result = await webClient.UpdateUser(account).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } updated)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            await LoadUsers().ConfigureAwait(true);
            return;
        }

        if (mainViewModel.User is { } me && string.Equals(me.UserName, updated.UserName, StringComparison.OrdinalIgnoreCase))
        {
            // The administrator changed their own account. The menu bar shows the new roles, and a new password
            // has to be logged in with right away: the server checks the credentials on every call, so the ones
            // the client still carries stopped working the moment the change was saved.
            mainViewModel.User = updated;

            if (account.Password is { } password)
            {
                var login = await webClient.Login(updated.UserName, password).ConfigureAwait(true);

                if (login.Status != HttpStatusCode.OK)
                {
                    await ShowHttpError(login).ConfigureAwait(true);
                    return;
                }

                await StoredConnection.SaveAsync(updated.UserName, password).ConfigureAwait(true);
            }
        }

        await LoadUsers().ConfigureAwait(true);
    });

    [RelayCommand(CanExecute = nameof(HasSelectedUser))]
    private Task Delete() => TaskExceptionHandler(async () =>
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        var answer = await new MessageBox
        {
            Text = string.Format(CultureInfo.CurrentCulture, Loc.ConfirmDeleteUser, user.UserName),
            Title = Loc.DeleteUser,
            Buttons = [Loc.Delete, Loc.Cancel],
            Icon = new WarningIcon(),
        }.Show().ConfigureAwait(true);

        if (answer?.Index != 0)
        {
            return;
        }

        BusyText = Loc.DeleteUser;
        var result = await webClient.DeleteUser(user.UserName).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK)
        {
            await ShowHttpError(result).ConfigureAwait(true);
        }

        await LoadUsers().ConfigureAwait(true);
    });

    private async Task LoadUsers()
    {
        BusyText = Loc.BusyLoadingUsers;
        var result = await webClient.GetUsers().ConfigureAwait(true);
        BusyText = null;

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } users)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        var selectedName = SelectedUser?.UserName;
        Users.Clear();
        users.Apply(Users.Add);
        SelectedUser = Users.FirstOrDefault(u => string.Equals(u.UserName, selectedName, StringComparison.OrdinalIgnoreCase));
    }
}