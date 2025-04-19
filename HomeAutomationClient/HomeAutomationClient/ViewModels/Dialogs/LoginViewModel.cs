using De.Hochstaetter.HomeAutomationClient.MessageBoxes;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

public partial class LoginViewModel(DialogParameters parameters) : DialogBase<DialogParameters, bool, LoginView>(parameters)
{
    private static readonly ICache cache = IoC.GetRegistered<ICache>();
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly IServerBasedAesKeyProvider keyProvider = IoC.GetRegistered<IServerBasedAesKeyProvider>();
    private HomeAutomationServerConnection? connection;

    [ObservableProperty, Required(AllowEmptyStrings = false)]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty, Required(AllowEmptyStrings = false)]
    public partial string Password { get; set; } = string.Empty;

    public ICommand? LoginCommand => field ??= new RelayCommand(() => _ = Login());

    public override async Task Initialize()
    {
        BusyText = Resources.ConnectingToHas;

        try
        {
            PropertyChanged += OnAnyPropertyChanged;
            await keyProvider.SetKeyFromUserName(null);
            UserName = (await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection))?.UserName ?? string.Empty;

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

            //await Login();
        }
        catch(Exception ex)
        {
            await ex.Show();
        }
        finally
        {
            BusyText = null;
            await base.Initialize();
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

            var problemDetails = await webClient.Login(UserName, Password);

            if (problemDetails != null)
            {
                await problemDetails.Show();
                return;
            }

            await keyProvider.SetKeyFromUserName(UserName);
            connection = IoC.GetRegistered<HomeAutomationServerConnection>();
            WebConnection.InvalidateKey();
            connection.UserName = UserName;
            connection.Password = Password;
            connection.BaseUrl = IoC.Get<MainViewModel>().ApiUri;
            await connection.UpdateChecksumAsync();
            await cache.AddOrUpdateAsync(CacheKeys.Connection, connection);

            var apiResult = await webClient.ListDevices();

            await new MessageBox
            {
                Icon = new InfoIcon(),
                Title = "Test",
                Text = "The following devices were found:",
                ItemList = apiResult.Payload?.Values.Select(d => $"{d.DeviceType}: {d.DisplayName}").ToList(),
            }.Show();

            Close();
        }
        catch (Exception ex)
        {
            await ex.Show();
        }
        finally
        {
            BusyText = null;
        }
    }
}
