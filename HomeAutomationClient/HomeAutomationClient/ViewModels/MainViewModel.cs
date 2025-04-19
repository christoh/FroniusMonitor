using System.Collections.Concurrent;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ICache? cache = IoC.TryGetRegistered<ICache>();

    public MainViewModel(IWebClientService webClient, IServerBasedAesKeyProvider keyProvider)
    {
        ApiUri = cache?.Get<string>(CacheKeys.ApiUri) ?? "https://home-automation.example.com";
        webClient.Initialize(ApiUri, "hacc", "0.9.0.0");
    }

    public string ApiUri { get; }

    [ObservableProperty]
    public partial object? MainViewContent { get; set; }

    public bool IsDialogVisible => CurrentDialog != null;

    [ObservableProperty,NotifyPropertyChangedFor(nameof(IsDialogVisible))]
    public partial DialogQueueItem? CurrentDialog { get; set; }
    
    public ConcurrentStack<DialogQueueItem?> DialogQueue { get; } = new();
    
    public ICommand? DialogClosedCommand => field ??= new RelayCommand(async void () =>
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
    });

    public override async Task Initialize()
    {
        var loginViewModel = new LoginViewModel(new DialogParameters
        {
            Title = $"{AppConstants.AppName} - {Resources.LoginNoun}",
            ShowCloseBox = false,
        });

        var isLoggedIn = await loginViewModel.ShowDialogAsync();
        await Dispatcher.UIThread.InvokeAsync(() => MainViewContent = new UiDemoView());
    }
}
