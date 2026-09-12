namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

public partial class LoginViewModel(DialogParameters parameters) : DialogBase<DialogParameters, bool, LoginView>(parameters)
{
    private static readonly ICache cache = IoC.GetRegistered<ICache>();
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly IServerBasedAesKeyProvider keyProvider = IoC.GetRegistered<IServerBasedAesKeyProvider>();

    [ObservableProperty, Required(AllowEmptyStrings = false)]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty, Required(AllowEmptyStrings = false)]
    public partial string Password { get; set; } = string.Empty;

    /// <summary>Who the server says logged in, with their roles. Null until a login succeeded.</summary>
    public UserInfo? User { get; private set; }

    public override async Task Initialize()
    {
        await base.Initialize();
        BusyText = Resources.CryptoInit;

        try
        {
            PropertyChanged += OnAnyPropertyChanged;
            await keyProvider.SetKeyFromUserName(null);
            UserName = (await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection))?.UserName ?? string.Empty;

            if (!string.IsNullOrEmpty(UserName))
            {
                await keyProvider.SetKeyFromUserName(UserName).ConfigureAwait(false);
                WebConnection.InvalidateKey();
                var cachedConnection = await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection).ConfigureAwait(false);

                if (cachedConnection != null)
                {
                    Password = cachedConnection.Password;
                }
            }
        }
        catch(Exception ex)
        {
            BusyText = null;
            await ex.Show();
        }
        finally
        {
            BusyText = null;
        }
    }

    private void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            ValidateAllProperties();
            return;
        }

        var propertyInfo = GetType().GetProperty(e.PropertyName);

        if (propertyInfo != null)
        {
            ValidateProperty(propertyInfo.GetValue(this), e.PropertyName);
        }
    }

    public override Task AbortAsync()
    {
        Result = false;
        Close();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Login()
    {
        try
        {
            ValidateAllProperties();

            if (HasErrors)
            {
                return;
            }

            BusyText = Resources.BusyLoggingIn;

            var result = await webClient.Login(UserName, Password);

            if (result.Status != HttpStatusCode.OK || result.Payload is not { } user)
            {
                await result.Show();
                return;
            }

            User = user;
            await StoredConnection.SaveAsync(UserName, Password);

            Result = true;
            Close();
        }
        catch (Exception ex)
        {
            BusyText = null;
            await ex.Show();
        }
        finally
        {
            BusyText = null;
        }
    }
}
