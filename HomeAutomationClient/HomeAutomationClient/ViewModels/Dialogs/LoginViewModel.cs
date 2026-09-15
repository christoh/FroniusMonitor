namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

public partial class LoginViewModel(DialogParameters parameters) : DialogBase<DialogParameters, bool, LoginView>(parameters)
{
    private static readonly ICache cache = IoC.GetRegistered<ICache>();
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private readonly IServerBasedAesKeyProvider keyProvider = IoC.GetRegistered<IServerBasedAesKeyProvider>();
    private bool isInitialized;

    [ObservableProperty, Required(AllowEmptyStrings = false)]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty, Required(AllowEmptyStrings = false)]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RememberPassword { get; set; } = true;

    /// <summary>
    /// The address the server is reached at, as the user would type it into a browser. Only the root:
    /// <see cref="ServerUris"/> puts the api and hub addresses together from it.
    /// </summary>
    [ObservableProperty, AbsoluteUri(AllowEmpty = false)]
    public partial string BaseUri { get; set; } = string.Empty;

    /// <summary>
    /// Whether the dialog is asking for the server address rather than for credentials. The two never show at
    /// once: one Ok button serves both, and which of them it means is this.
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsLoggingIn), nameof(CanChooseServer))]
    public partial bool IsChoosingServer { get; set; }

    /// <summary>The other half of <see cref="IsChoosingServer"/>, for the boxes that are hidden while it is set.</summary>
    public bool IsLoggingIn => !IsChoosingServer;

    /// <summary>
    /// Whether the user may change the server address at all. A browser serves the client from the server it talks
    /// to, so there is nothing to choose there.
    /// </summary>
    /// <remarks>An instance property, not a static one: compiled bindings cannot read a static member.</remarks>
    public bool CanChangeConnection => !OperatingSystem.IsBrowser();

    /// <summary>Whether the "Choose server" button is on screen. It has nothing to offer while it is showing.</summary>
    public bool CanChooseServer => CanChangeConnection && !IsChoosingServer;

    /// <summary>Who the server says logged in, with their roles. Null until a login succeeded.</summary>
    public UserInfo? User { get; private set; }

    /// <summary>
    /// Loads what the cache knows, or asks for the server address where it knows nothing usable.
    /// </summary>
    /// <remarks>
    /// Runs again every time the body is re-attached, which is whenever a message box has opened and closed over
    /// this dialog - a wrong address or a refused login. Without the guard that would throw away what the user has
    /// typed and put the busy overlay back up while they are reading the error.
    /// </remarks>
    public override async Task Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        // Set before the first await, so a second call cannot get past the guard while the first is still running.
        isInitialized = true;

        await base.Initialize();
        PropertyChanged += OnAnyPropertyChanged;
        BaseUri = ServerUris.RootOf(cache.Get<string>(CacheKeys.ApiUri)) ?? string.Empty;

        if (!ServerUris.AreUsable(cache.Get<string>(CacheKeys.ApiUri), cache.Get<string>(CacheKeys.HubUri)))
        {
            // Nothing to log in to yet - a phone that has never been told where the server is, or a leftover from
            // one that has moved. Asking for the address is the answer, not an error box about it.
            IsChoosingServer = CanChangeConnection;
            return;
        }

        await TaskExceptionHandler(async () =>
        {
            if (await LoadCachedCredentialsAsync().ConfigureAwait(true) is { } problem)
            {
                // The address the cache holds leads nowhere. Say so once, then ask for a better one.
                await problem.ShowServerProblem(BaseUri).ConfigureAwait(true);
                IsChoosingServer = CanChangeConnection;
            }
        });
    }

    /// <summary>
    /// Puts the cached credentials into the boxes and fetches the key of the server, which is what the cached
    /// password is encrypted with. The cache is read twice: the user name comes out under the all-zero key, and
    /// only with the user name can the server be asked for the real one.
    /// </summary>
    /// <remarks>
    /// **Only empty boxes are filled.** This runs again after the user has chosen another server, and by then
    /// they may well have typed the credentials for it already - taking those away to put a cached password of
    /// the previous server in their place would be the last thing they asked for.
    /// </remarks>
    /// <returns><see langword="null"/> where the server answered, and what went wrong otherwise.</returns>
    private async Task<ProblemDetails?> LoadCachedCredentialsAsync()
    {
        BusyText = Resources.CryptoInit;

        try
        {
            await keyProvider.SetKeyFromUserName(null).ConfigureAwait(true);
            WebConnection.InvalidateKey();
            var cachedUserName = (await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection).ConfigureAwait(true))?.UserName;

            if (string.IsNullOrEmpty(cachedUserName))
            {
                return null;
            }

            if (await keyProvider.SetKeyFromUserName(cachedUserName).ConfigureAwait(true) is { } problem)
            {
                return problem;
            }

            WebConnection.InvalidateKey();
            var cachedConnection = await cache.GetAsync<HomeAutomationServerConnection>(CacheKeys.Connection).ConfigureAwait(true);

            if (string.IsNullOrEmpty(UserName))
            {
                UserName = cachedUserName;
            }

            if (string.IsNullOrEmpty(Password))
            {
                // A password encrypted for another server cannot be decrypted with the key this one hands out, so
                // after a change of server this comes back empty - and an empty box stays an empty box.
                Password = cachedConnection?.Password ?? string.Empty;
                RememberPassword = !string.IsNullOrEmpty(Password);
            }

            return null;
        }
        finally
        {
            BusyText = null;
        }
    }

    /// <summary>
    /// Takes the address the user typed: stores it, points the client at that server and fetches its key, because
    /// the key of the previous one says nothing about this one.
    /// </summary>
    /// <returns>Whether the client now has a server it can talk to.</returns>
    private async Task<bool> ApplyConnectionAsync()
    {
        if (ServerUris.From(BaseUri) is not { } uris)
        {
            return false;
        }

        if (uris.ApiUri != cache.Get<string>(CacheKeys.ApiUri) || uris.HubUri != cache.Get<string>(CacheKeys.HubUri))
        {
            await cache.AddOrUpdateAsync(CacheKeys.ApiUri, uris.ApiUri).ConfigureAwait(true);
            await cache.AddOrUpdateAsync(CacheKeys.HubUri, uris.HubUri).ConfigureAwait(true);
            IoC.Get<MainViewModel>().SetApiUri(uris.ApiUri);
        }

        if (await LoadCachedCredentialsAsync().ConfigureAwait(true) is { } problem)
        {
            // Staying in this mode is the point: the address is the thing the user can still do something about.
            await problem.ShowServerProblem(BaseUri).ConfigureAwait(true);
            return false;
        }

        IsChoosingServer = false;
        return true;
    }

    /// <summary>Shows the address box instead of the credentials. The Ok button then means "use this server".</summary>
    [RelayCommand]
    private void ChooseServer() => IsChoosingServer = true;

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

    /// <summary>
    /// The one Ok button of the dialog. What it does is whatever the dialog is currently asking for: the server
    /// address, or the credentials.
    /// </summary>
    [RelayCommand]
    private Task Ok() => TaskExceptionHandler(async () =>
    {
        if (!IsVisibleInputValid())
        {
            return;
        }

        if (IsChoosingServer)
        {
            await ApplyConnectionAsync().ConfigureAwait(true);
            return;
        }

        await LoginAsync().ConfigureAwait(true);
    });

    /// <summary>
    /// Validates the boxes that are on screen, and says whether all of them are filled in properly.
    /// </summary>
    /// <remarks>
    /// Only those. An error on a box the user cannot see is one they cannot correct, and validating everything
    /// would leave the credentials sitting there in red the moment the dialog comes back from choosing a server -
    /// complaining about an empty password they were never given the chance to type.
    /// </remarks>
    private bool IsVisibleInputValid()
    {
        if (IsChoosingServer)
        {
            ValidateProperty(BaseUri, nameof(BaseUri));
            return !GetErrors(nameof(BaseUri)).Any();
        }

        ValidateProperty(UserName, nameof(UserName));
        ValidateProperty(Password, nameof(Password));
        return !GetErrors(nameof(UserName)).Any() && !GetErrors(nameof(Password)).Any();
    }

    private async Task LoginAsync()
    {
        BusyText = Resources.BusyLoggingIn;

        var result = await webClient.Login(UserName, Password).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } user)
        {
            BusyText = null;
            await result.ShowServerProblem(BaseUri).ConfigureAwait(true);
            return;
        }

        User = user;

        if (await StoredConnection.SaveAsync(UserName, RememberPassword ? Password : string.Empty).ConfigureAwait(true) is { } problem)
        {
            // The login itself worked, so the user is in; only remembering them for next time did not.
            BusyText = null;
            await problem.ShowServerProblem(BaseUri).ConfigureAwait(true);
        }

        Result = true;
        Close();
    }
}
