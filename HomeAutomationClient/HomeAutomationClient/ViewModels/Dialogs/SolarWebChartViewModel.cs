namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The Solar.web chart dialog: the four views of Solar.web's chart page - production, consumption, and with a
/// Premium account profitability and costs - over a day, a month, a year or the whole history, for any date.
/// </summary>
/// <remarks>
/// <para>
/// Nothing is computed from a service here. Every choice is one request to the server, which answers from its
/// cache or asks Solar.web; the view model only decides what to ask for and works the
/// <see cref="SolarWebChartModel"/> out of the answer, and the view draws that.
/// </para>
/// <para>
/// The two Premium views have no day chart on Solar.web, so choosing the day switches a Premium view back to
/// production and keeps the two Premium buttons disabled while the day is shown. The whole history has no date.
/// </para>
/// </remarks>
public sealed partial class SolarWebChartViewModel(DialogParameters parameters) : DialogBase<DialogParameters, bool, SolarWebChartView>(parameters)
{
    /// <summary>Before this there was no Solar.web; the picker offers nothing earlier.</summary>
    private static readonly DateTime minimumDate = new(2000, 1, 1);

    /// <summary>Solar.web forecasts two days ahead, and each of them is a day chart of its own; nothing lies beyond.</summary>
    private static DateTime MaximumDate => DateTime.Today.AddDays(2);

    private readonly IWebClientService webClient = IoC.GetRegistered<IWebClientService>();
    private bool isInitialized;

    /// <summary>Counts the loads, so that the answer to a choice the user has already left behind is dropped.</summary>
    private int loadNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDay), nameof(IsMonth), nameof(IsYear), nameof(IsAll), nameof(CanChoosePremium), nameof(HasDate))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand), nameof(NextCommand))]
    public partial SolarWebInterval Interval { get; set; } = SolarWebInterval.Day;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProduction), nameof(IsConsumption), nameof(IsReturnOfInvestment), nameof(IsExpense))]
    public partial SolarWebView View { get; set; } = SolarWebView.Production;

    public bool IsDay { get => Interval == SolarWebInterval.Day; set => Choose(value, SolarWebInterval.Day); }

    public bool IsMonth { get => Interval == SolarWebInterval.Month; set => Choose(value, SolarWebInterval.Month); }

    public bool IsYear { get => Interval == SolarWebInterval.Year; set => Choose(value, SolarWebInterval.Year); }

    public bool IsAll { get => Interval == SolarWebInterval.All; set => Choose(value, SolarWebInterval.All); }

    public bool IsProduction { get => View == SolarWebView.Production; set => Choose(value, SolarWebView.Production); }

    public bool IsConsumption { get => View == SolarWebView.Consumption; set => Choose(value, SolarWebView.Consumption); }

    public bool IsReturnOfInvestment { get => View == SolarWebView.ReturnOfInvestment; set => Choose(value, SolarWebView.ReturnOfInvestment); }

    public bool IsExpense { get => View == SolarWebView.Expense; set => Choose(value, SolarWebView.Expense); }

    /// <summary>The Premium views exist for a month, a year and the whole history, not for a day.</summary>
    public bool CanChoosePremium => Interval != SolarWebInterval.Day;

    /// <summary>The whole history has no date to pick.</summary>
    public bool HasDate => Interval != SolarWebInterval.All;

    /// <summary>
    /// The date shown: the day, or a day of the month or the year. A <see cref="DateTimeOffset"/> because that is
    /// what the date picker produces, and the picker may bind the value directly: it cannot be typed into. Kept
    /// between <see cref="minimumDate"/> and <see cref="MaximumDate"/> by its changed handler, because the picker has no
    /// bounds of its own.
    /// </summary>
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(PreviousCommand), nameof(NextCommand))]
    public partial DateTimeOffset? Date { get; set; } = new DateTimeOffset(DateTime.Today);

    public DateTimeOffset MinimumYear => new(minimumDate);

    public DateTimeOffset MaximumYear => new(MaximumDate);

    /// <summary>What the view draws, or <see langword="null"/> while there is nothing to draw.</summary>
    [ObservableProperty]
    public partial SolarWebChartModel? ChartModel { get; set; }

    /// <summary>True where Solar.web says the view needs a Premium subscription the account does not have; the view shows a hint.</summary>
    [ObservableProperty]
    public partial bool IsPremiumFeature { get; set; }

    public override Task Initialize()
    {
        if (isInitialized)
        {
            return Task.CompletedTask;
        }

        isInitialized = true;
        return Load();
    }

    partial void OnIntervalChanged(SolarWebInterval value)
    {
        if (value == SolarWebInterval.Day && View.IsPremium())
        {
            // Sets the view, whose changed handler loads.
            View = SolarWebView.Production;
            return;
        }

        LoadGuarded();
    }

    partial void OnViewChanged(SolarWebView value) => LoadGuarded();

    partial void OnDateChanged(DateTimeOffset? value)
    {
        if (value is { } date && (date.Date < minimumDate || date.Date > MaximumDate))
        {
            // Sets the property again, which comes back here with a value inside the bounds.
            Date = new DateTimeOffset(date.Date < minimumDate ? minimumDate : MaximumDate);
            return;
        }

        if (HasDate)
        {
            LoadGuarded();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToPrevious))]
    private void Previous() => Date = Step(-1);

    private bool CanGoToPrevious() => HasDate && Step(-1) is { } stepped && stepped.Date >= minimumDate;

    [RelayCommand(CanExecute = nameof(CanGoToNext))]
    private void Next() => Date = Step(1);

    private bool CanGoToNext() => HasDate && Step(1) is { } stepped && stepped.Date <= MaximumDate;

    [RelayCommand]
    private void CloseDialog()
    {
        Result = true;
        Close();
    }

    /// <summary>The close box of the frame. Nothing to unwind: the chart writes nothing anywhere.</summary>
    public override Task AbortAsync()
    {
        CloseDialog();
        return Task.CompletedTask;
    }

    /// <summary>A radio button turning on chooses; one turning off says nothing, the one turning on does.</summary>
    private void Choose(bool on, SolarWebInterval interval)
    {
        if (on)
        {
            Interval = interval;
        }
    }

    private void Choose(bool on, SolarWebView view)
    {
        if (on)
        {
            View = view;
        }
    }

    /// <summary>The date one period on, in the direction given: a day, a month or a year.</summary>
    private DateTimeOffset? Step(int direction) => Interval switch
    {
        SolarWebInterval.Day => Date?.AddDays(direction),
        SolarWebInterval.Month => Date?.AddMonths(direction),
        SolarWebInterval.Year => Date?.AddYears(direction),
        _ => Date,
    };

    /// <summary>Nothing awaits a changed handler; the load reports its own failures through TaskExceptionHandler.</summary>
    private void LoadGuarded()
    {
        if (isInitialized)
        {
            _ = Load();
        }
    }

    private Task Load() => TaskExceptionHandler(async () =>
    {
        var number = ++loadNumber;
        BusyText = Loc.LoadingSolarWebData;
        var result = await webClient.GetSolarWebChart(Interval, View, DateOnly.FromDateTime(Date?.Date ?? DateTime.Today)).ConfigureAwait(true);

        if (number != loadNumber)
        {
            // The user has chosen something else meanwhile; that load will clear the busy text.
            return;
        }

        BusyText = null;

        if (result.Status != HttpStatusCode.OK || result.Payload is not { } chart)
        {
            ChartModel = null;
            IsPremiumFeature = false;
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        IsPremiumFeature = chart.IsPremiumFeature;
        ChartModel = SolarWebChartModel.Build(chart);
    });
}
