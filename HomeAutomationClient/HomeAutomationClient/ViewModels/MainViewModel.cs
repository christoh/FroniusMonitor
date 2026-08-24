using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using InverterDetailsView = De.Hochstaetter.HomeAutomationClient.Views.InverterDetailsView;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IGen24LocalizationService gen24Loc;

    public IUpdateService UpdateService { get; }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public MainViewModel(IWebClientService webClient, IGen24LocalizationService gen24Loc, IUpdateService updateService)
    {
        this.gen24Loc = gen24Loc;
        UpdateService = updateService;
        ApiUri = IoC.TryGetRegistered<ICache>()?.Get<string>(CacheKeys.ApiUri) ?? "https://home-automation.example.com";
        webClient.Initialize(ApiUri, "hacc", "0.5.0.0");
    }

    public string ApiUri { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsDialogBusy))]
    public partial string? DialogBusyText { get; set; }

    public bool IsDialogBusy => DialogBusyText != null && IsDialogVisible;

    [ObservableProperty]
    public partial bool IsReady { get; set; }

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
            BusyText = Loc.ConnectingToHas;
            await UpdateService.StartAsync().ConfigureAwait(false);
            IsReady = true;
            await Dispatcher.UIThread.InvokeAsync(() => MainViewContent = IoC.Get<DashboardView>());
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

    [RelayCommand]
    private async Task ShowDetails(IKeyedDevice device)
    {
        switch (device.Device)
        {
            case Gen24System gen24System:
                var detailsView = IoC.Get<InverterDetailsView>();
                detailsView.ViewModel.Gen24System = gen24System;
                MainViewContent = detailsView;
                break;

            default:
                await new MessageBox
                {
                    Text = $"Details view for device type {device.Device.GetType().Name} is not implemented.",
                    Title = "NotImplemented",
                    Icon = new ErrorIcon(),
                }.Show().ConfigureAwait(false);

                break;
        }
    }

    [RelayCommand]
    private async Task ShowDashboard()
    {
        try
        {
            BusyText = "Loading dashboard";
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            var dashboardView = IoC.Get<DashboardView>();
            await Task.Delay(100).ConfigureAwait(false);
            MainViewContent = dashboardView;
        }
        finally
        {
            BusyText = null;
        }
    }
}
