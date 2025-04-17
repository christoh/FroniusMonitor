using System.Net;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationClient.Adapters;
using De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

public partial class LoginViewModel(string title, object? parameters = null) : DialogBase<object?, bool, LoginView>(title, parameters)
{
    private static readonly ICache cache = IoC.GetRegistered<ICache>();
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly IServerBasedAesKeyProvider keyProvider = IoC.GetRegistered<IServerBasedAesKeyProvider>();
    private HomeAutomationServerConnection? connection;

    [ObservableProperty] private string? userName = cache.Get<string>(CacheKeys.UserName);

    [ObservableProperty] private string? password;

    [ObservableProperty] private bool isAuthenticated;

    [ObservableProperty] private bool isInitialized;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoginFailed))]
    private string? loginFailedMessage;

    public bool LoginFailed => LoginFailedMessage != null;

    public ICommand? LoginCommand => field ??= new RelayCommand(() => _ = Login());

    public override async Task Initialize()
    {
        try
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                await keyProvider.SetKeyFromUserName(UserName);
                WebConnection.InvalidateKey();
                var cachedConnection = await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection);

                if (cachedConnection != null)
                {
                    Password = cachedConnection.Password;
                }
            }

            if (string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(UserName))
            {
                return;
            }

            await Login().ConfigureAwait(false);
        }
        finally
        {
            await base.Initialize().ConfigureAwait(false);
            IsInitialized = true;
        }
    }

    public override Task AbortAsync()
    {
        Result = false;
        Close();
        return Task.CompletedTask;
    }

    private async Task Login()
    {
        try
        {
            if (string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(UserName))
            {
                return;
            }

            var result = await webClient.Login(UserName, Password).ConfigureAwait(false);
            LoginFailedMessage = result == HttpStatusCode.OK ? null : result + $" ({(int)result})";

            if (!LoginFailed)
            {
                await cache.AddOrUpdateAsync(CacheKeys.UserName, UserName).ConfigureAwait(false);
                await keyProvider.SetKeyFromUserName(UserName).ConfigureAwait(false);
                connection = IoC.GetRegistered<HomeAutomationServerConnection>();
                WebConnection.InvalidateKey();
                connection.UserName = UserName;
                connection.Password = Password;
                connection.BaseUrl = IoC.Get<MainViewModel>().ApiUri;
                connection.PasswordChecksum = connection.CalculatedChecksum;
                await cache.AddOrUpdateAsync(CacheKeys.Connection, connection);
                Close();
            }
        }
        catch (Exception ex)
        {
            LoginFailedMessage = ex.Message;
        }
    }
}
