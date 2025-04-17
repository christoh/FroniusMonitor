using Avalonia.Threading;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ICache? cache = IoC.TryGetRegistered<ICache>();

    public MainViewModel(IWebClientService webClient, IServerBasedAesKeyProvider keyProvider)
    {
        ApiUri = cache?.Get<string>(CacheKeys.ApiUri) ?? "https://home-automation.example.com";
        webClient.Initialize(ApiUri);
    }

    public string ApiUri { get; }

    [ObservableProperty] public partial object? MainViewContent { get; set; }

    [ObservableProperty] public partial bool IsDialogVisible { get; set; }

    [ObservableProperty] public partial object? DialogContent { get; set; }

    [ObservableProperty] public partial string? TitleText { get; set; }


    public ICommand? DialogClosedCommand => field ??= new RelayCommand(async void () =>
    {
        try
        {
            if (DialogContent is IDialogControl { DataContext: IDialogBase dialogBase })
            {
                await dialogBase.AbortAsync().ConfigureAwait(false);
                return;
            }

            IsDialogVisible = false;
        }
        catch
        {
            // async void must be caught to avoid unhandled exceptions
        }
    });

    public override async Task Initialize()
    {
        var loginViewModel = new LoginViewModel("Home Automation Control Center - Login");
        var isLoggedIn = await loginViewModel.ShowDialogAsync();
        await Dispatcher.UIThread.InvokeAsync(() => MainViewContent = new UiDemoView());
    }
}
