using System.Collections.Concurrent;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ICache? cache = IoC.TryGetRegistered<ICache>();
    private readonly IWebClientService webClient;
    private readonly IGen24LocalizationService gen24Loc;

    public MainViewModel(IWebClientService webClient, IServerBasedAesKeyProvider keyProvider, IGen24LocalizationService gen24Loc)
    {
        this.webClient = webClient;
        this.gen24Loc = gen24Loc;
        ApiUri = cache?.Get<string>(CacheKeys.ApiUri) ?? "https://home-automation.example.com";
        webClient.Initialize(ApiUri, "hacc", "0.5.0.0");
    }

    public string ApiUri { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsDialogBusy))]
    public partial string? DialogBusyText { get; set; }

    public bool IsDialogBusy => DialogBusyText != null && IsDialogVisible;

    [ObservableProperty]
    public partial object? MainViewContent { get; set; }

    public bool IsDialogVisible => CurrentDialog != null;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsDialogVisible), nameof(IsDialogBusy))]
    public partial DialogQueueItem? CurrentDialog { get; set; }

    public ConcurrentStack<DialogQueueItem?> DialogQueue { get; } = new();

    [RelayCommand]
    public async Task DialogClosed()
    {
        try
        {
            if (CurrentDialog?.Body is IDialogControl { DataContext: IDialogBase dialogBase })
            {
                await dialogBase.AbortAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // async void must be caught to avoid unhandled exceptions
        }
    }

    public override async Task Initialize()
    {
        try
        {
            await base.Initialize();


            var loginViewModel = new LoginViewModel(new DialogParameters
            {
                Title = $"{AppConstants.AppName} - {Resources.LoginNoun}",
                ShowCloseBox = false,
            });

            var isLoggedIn = await loginViewModel.ShowDialogAsync();
            BusyText= Resources.Loading;
            await gen24Loc.Initialize().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => MainViewContent = new UiDemoView());
        }
        finally
        {
            BusyText = null;
        }
    }
}
