namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The price chart dialog, ported from <c>PriceViewModel</c> of FroniusMonitor: buying or market price, net or
/// gross, today, tomorrow or a day of the history, with the sun and wind production of the grid and the weather at
/// the server's DWD station over it.
/// </summary>
/// <remarks>
/// <para>
/// Nothing is computed from a service here. The server assembles today and tomorrow and pushes them over the hub,
/// <see cref="IUpdateService.EnergyChartData"/> holds the latest, and a historic day is one request to the server,
/// which answers from its history. The view model only decides which day and which price to show and works the
/// <see cref="EnergyChartModel"/> out of it; the view draws that.
/// </para>
/// <para>
/// <see cref="Initialize"/> runs again whenever the body is re-attached - the price components open as a dialog
/// over this one - so it is guarded like the settings dialog's; see the dialog system contract.
/// </para>
/// </remarks>
public sealed partial class EnergyChartViewModel(DialogParameters parameters) : DialogBase<DialogParameters, bool, EnergyChartView>(parameters)
{
    /// <summary>The first day Awattar has prices for.</summary>
    private static readonly DateTime minimumDate = new(2013, 12, 22);

    private readonly IUpdateService updateService = IoC.GetRegistered<IUpdateService>();
    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private bool isInitialized;

    /// <summary>What the server answered when the update service had nothing yet; a push replaces it.</summary>
    private EnergyChartData? fetchedLiveData;

    private EnergyChartData? historicData;

    private EnergyChartData? LiveData => updateService.EnergyChartData ?? fetchedLiveData;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsMarketPrice))]
    public partial bool IsBuyingPrice { get; set; } = true;

    public bool IsMarketPrice
    {
        get => !IsBuyingPrice;
        set => IsBuyingPrice = !value;
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsNet))]
    public partial bool IsGross { get; set; } = true;

    public bool IsNet
    {
        get => !IsGross;
        set => IsGross = !value;
    }

    [ObservableProperty]
    public partial bool IsToday { get; set; } = true;

    [ObservableProperty]
    public partial bool IsTomorrow { get; set; }

    [ObservableProperty]
    public partial bool IsHistoric { get; set; }

    [ObservableProperty]
    public partial bool ShowProductions { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowWeather { get; set; } = true;

    /// <summary>
    /// The day of the history shown while <see cref="IsHistoric"/>. Yesterday to begin with. A <see cref="DateTimeOffset"/>
    /// because that is what the date picker produces, and the picker may bind the value directly: it cannot be typed
    /// into, so it can only ever produce a date - see the interaction rule. Kept between <see cref="MinimumDate"/> and
    /// <see cref="MaximumDate"/> by its changed handler, because the picker has no bounds of its own.
    /// </summary>
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(PreviousDayCommand), nameof(NextDayCommand))]
    public partial DateTimeOffset? HistoricDate { get; set; } = new DateTimeOffset(DateTime.Today.AddDays(-1));

    public DateTime MinimumDate => minimumDate;

    /// <summary>Yesterday: today and tomorrow have their own buttons and come from the live data.</summary>
    public DateTime MaximumDate => DateTime.Today.AddDays(-1);

    /// <summary>
    /// The years the picker offers. A <c>DatePicker</c> bounds only the year, not the day - it has MinYear and
    /// MaxYear where the WPF picker had a first and a last date - so the changed handler of <see cref="HistoricDate"/>
    /// keeps the day itself inside <see cref="MinimumDate"/> and <see cref="MaximumDate"/>.
    /// </summary>
    public DateTimeOffset MinimumYear => new(minimumDate);

    public DateTimeOffset MaximumYear => new(MaximumDate);

    /// <summary>What the view draws, or <see langword="null"/> while there is nothing to draw.</summary>
    [ObservableProperty]
    public partial EnergyChartModel? ChartModel { get; set; }

    /// <summary>The tariff components of the day shown, for the components dialog.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasPriceComponents)), NotifyCanExecuteChangedFor(nameof(ShowPriceComponentsCommand))]
    public partial IReadOnlyList<EnergyPriceComponent> PriceComponents { get; set; } = [];

    public bool HasPriceComponents => PriceComponents.Count > 0;

    public override Task Initialize()
    {
        if (isInitialized)
        {
            return Task.CompletedTask;
        }

        isInitialized = true;
        updateService.EnergyChartDataChanged += OnEnergyChartDataChanged;

        return TaskExceptionHandler(async () =>
        {
            if (LiveData == null)
            {
                BusyText = Loc.LoadingEnergyData;
                var result = await webClient.GetEnergyData().ConfigureAwait(true);
                BusyText = null;

                if (result.Status != HttpStatusCode.OK || result.Payload is not { } data)
                {
                    Rebuild();
                    await ShowHttpError(result).ConfigureAwait(true);
                    return;
                }

                fetchedLiveData = data;
            }

            Rebuild();
        });
    }

    partial void OnIsBuyingPriceChanged(bool value) => Rebuild();

    partial void OnIsGrossChanged(bool value) => Rebuild();

    partial void OnShowProductionsChanged(bool value) => Rebuild();

    partial void OnShowWeatherChanged(bool value) => Rebuild();

    partial void OnIsTodayChanged(bool value)
    {
        if (value)
        {
            Rebuild();
        }
    }

    partial void OnIsTomorrowChanged(bool value)
    {
        if (value)
        {
            Rebuild();
        }
    }

    partial void OnIsHistoricChanged(bool value)
    {
        if (value)
        {
            // Nothing awaits this; TaskExceptionHandler inside LoadHistoric reports what goes wrong.
            _ = LoadHistoric();
        }
    }

    partial void OnHistoricDateChanged(DateTimeOffset? value)
    {
        if (value is { } date && (date.Date < MinimumDate || date.Date > MaximumDate))
        {
            // Sets the property again, which comes back here with a value inside the bounds.
            HistoricDate = new DateTimeOffset(date.Date < MinimumDate ? MinimumDate : MaximumDate);
            return;
        }

        if (IsHistoric)
        {
            _ = LoadHistoric();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousDay))]
    private void PreviousDay() => HistoricDate = HistoricDate?.AddDays(-1);

    private bool CanGoToPreviousDay() => HistoricDate is { } date && date.Date > MinimumDate;

    [RelayCommand(CanExecute = nameof(CanGoToNextDay))]
    private void NextDay() => HistoricDate = HistoricDate?.AddDays(1);

    private bool CanGoToNextDay() => HistoricDate is { } date && date.Date < MaximumDate;

    [RelayCommand(CanExecute = nameof(HasPriceComponents))]
    private Task ShowPriceComponents() => TaskExceptionHandler(async () =>
    {
        await new PriceComponentsViewModel(new DialogParameters { Title = Loc.PriceComponents }, PriceComponents).ShowDialogAsync().ConfigureAwait(true);
    });

    [RelayCommand]
    private void CloseDialog()
    {
        updateService.EnergyChartDataChanged -= OnEnergyChartDataChanged;
        Result = true;
        Close();
    }

    /// <summary>The close box of the frame. Nothing to unwind: the chart writes nothing anywhere.</summary>
    public override Task AbortAsync()
    {
        CloseDialog();
        return Task.CompletedTask;
    }

    /// <summary>
    /// A push from the server. Arrives on the hub's thread; the properties it ends in are bound, and the view
    /// marshals the redraw itself.
    /// </summary>
    private void OnEnergyChartDataChanged(object? sender, EnergyChartData data)
    {
        if (!IsHistoric)
        {
            Rebuild();
        }
    }

    private Task LoadHistoric() => TaskExceptionHandler(async () =>
    {
        if (HistoricDate is not { } date)
        {
            return;
        }

        BusyText = Loc.LoadingEnergyData;
        var result = await webClient.GetEnergyData(DateOnly.FromDateTime(date.Date)).ConfigureAwait(true);
        BusyText = null;

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } data)
        {
            historicData = null;
            Rebuild();
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        historicData = data;
        Rebuild();
    });

    /// <summary>
    /// Works the chart model out of the data of the day shown. Today and tomorrow are the client's local days cut
    /// out of the live span; a historic day is the server's day as it answered it.
    /// </summary>
    private void Rebuild()
    {
        var data = IsHistoric ? historicData : LiveData;
        var today = DateTime.Today;
        DateTime dayStart, dayEnd;

        if (IsHistoric && data != null)
        {
            dayStart = DateTime.SpecifyKind(data.From, DateTimeKind.Utc).ToLocalTime();
            dayEnd = DateTime.SpecifyKind(data.To, DateTimeKind.Utc).ToLocalTime();
        }
        else
        {
            dayStart = IsTomorrow ? today.AddDays(1) : today;
            dayEnd = dayStart.AddDays(1);
        }

        if (data == null)
        {
            PriceComponents = [];
            ChartModel = null;
            return;
        }

        // What is missing - no weather for a historic day, no components without the tariff query - is not
        // announced: the chart shows what there is, and an empty axis says enough.
        PriceComponents = data.PriceComponents;
        ChartModel = EnergyChartModel.Build(data, dayStart, dayEnd, IsBuyingPrice ? EnergyPriceDisplay.Buy : EnergyPriceDisplay.Market, IsGross, ShowProductions, ShowWeather);
    }
}
