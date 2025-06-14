using System.Collections.Concurrent;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ICache? cache = IoC.TryGetRegistered<ICache>();
    private readonly IGen24LocalizationService gen24Loc;

    public MainViewModel(IWebClientService webClient, IGen24LocalizationService gen24Loc)
    {
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
        catch (Exception ex)
        {
            await ex.Show().ConfigureAwait(false);
        }
    }

    public override async Task Initialize()
    {
        try
        {
            await base.Initialize().ConfigureAwait(false);

            var loginViewModel = new LoginViewModel(new DialogParameters
            {
                Title = $"{AppConstants.AppName} - {Loc.LoginNoun}",
                ShowCloseBox = false,
            });

            await loginViewModel.ShowDialogAsync().ConfigureAwait(false);
            BusyText = Loc.GetInverterLocalization;
            await gen24Loc.Initialize().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => MainViewContent = new UiDemoView());
        }
        catch (Exception ex)
        {
            BusyText = null;
            await ex.Show().ConfigureAwait(false);
        }
        finally
        {
            BusyText = null;
        }
    }
}
