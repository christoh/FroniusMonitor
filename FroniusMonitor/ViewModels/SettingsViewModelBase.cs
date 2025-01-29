namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public abstract class SettingsViewModelBase : ViewModelBase
{
    protected readonly IGen24JsonService Gen24JsonService;
    protected readonly IWattPilotService WattPilotService;

    protected SettingsViewModelBase(IDataCollectionService dataCollectionService, IGen24Service gen24Service,
        IGen24JsonService gen24JsonService, IFritzBoxService fritzBoxService, IWattPilotService wattPilotService)
    {
        Gen24Service = gen24Service;
        Gen24JsonService = gen24JsonService;
        FritzBoxService = fritzBoxService;
        WattPilotService = wattPilotService;
        DataCollectionService = dataCollectionService;
    }

    public IDataCollectionService DataCollectionService
    {
        get;
        set => Set(ref field, value);
    } = null!;

    public IGen24Service Gen24Service { get; }
    public IFritzBoxService FritzBoxService { get; }

    public string ToastText
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public bool IsInUpdate
    {
        get;
        set => Set(ref field, value);
    }

    protected async Task<bool> UpdateInverter(string uri, JToken token)
    {
        (JToken Token, HttpStatusCode Status) result;

        try
        {
            result = await Gen24Service.GetFroniusJsonResponse(uri, token, [HttpStatusCode.OK, HttpStatusCode.BadRequest]).ConfigureAwait(false);
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