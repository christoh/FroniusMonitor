namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public abstract class SettingsViewModelBase : ViewModelBase
{
    protected readonly IGen24JsonService Gen24Service;
    protected readonly IWattPilotService WattPilotService;

    protected SettingsViewModelBase(IDataCollectionService dataCollectionService, IWebClientService webClientService, IGen24JsonService gen24Service, IWattPilotService wattPilotService)
    {
        WebClientService = webClientService;
        Gen24Service = gen24Service;
        WattPilotService = wattPilotService;
        DataCollectionService = dataCollectionService;
    }

    private IDataCollectionService dataCollectionService = null!;

    public IDataCollectionService DataCollectionService
    {
        get => dataCollectionService;
        set => Set(ref dataCollectionService, value);
    }

    public IWebClientService WebClientService { get; }

    private string toastText = string.Empty;

    public string ToastText
    {
        get => toastText;
        set => Set(ref toastText, value);
    }

    private bool isInUpdate;

    public bool IsInUpdate
    {
        get => isInUpdate;
        set => Set(ref isInUpdate, value);
    }

    protected async Task<bool> UpdateInverter(string uri, JToken token)
    {
        (JToken Token, HttpStatusCode Status) result;

        try
        {
            result = await WebClientService.GetFroniusJsonResponse(uri, token, new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            IsInUpdate = false;

            ShowBox
            (
                string.Format(Loc.InverterCommError, ex.Message) + Environment.NewLine + Environment.NewLine + token,
                ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
            );

            return false;
        }

        if (result.Status == HttpStatusCode.OK)
        {
            return true;
        }

        IsInUpdate = false;

        ShowBox
        (
            string.Format(Loc.InverterCommError, Loc.InvalidParameters) + Environment.NewLine + Environment.NewLine + token + Environment.NewLine + Environment.NewLine + result.Token,
            Loc.InvalidParameters, MessageBoxButton.OK, MessageBoxImage.Error
        );

        return false;
    }

    protected void ShowNoSettingsChanged()
    {
        IsInUpdate = false;
        ShowBox(Loc.NoSettingsChanged, Loc.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}