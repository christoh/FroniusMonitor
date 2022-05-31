using System.Net;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public abstract class SettingsViewModelBase : ViewModelBase
{
    protected readonly IWebClientService WebClientService;
    protected readonly IGen24JsonService Gen24Service;

    protected SettingsViewModelBase(IWebClientService webClientService, IGen24JsonService gen24Service)
    {
        WebClientService = webClientService;
        Gen24Service = gen24Service;
    }

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

            await Dispatcher.InvokeAsync(() => MessageBox.Show
            (
                string.Format(Resources.InverterCommError, ex.Message) + Environment.NewLine + Environment.NewLine + token,
                ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
            ));

            return false;
        }

        if (result.Status == HttpStatusCode.OK)
        {
            return true;
        }

        IsInUpdate = false;

        await Dispatcher.InvokeAsync(() => MessageBox.Show
        (
            string.Format(Resources.InverterCommError, Resources.InvalidParameters) + Environment.NewLine + Environment.NewLine + token + Environment.NewLine + Environment.NewLine + result.Token,
            Resources.InvalidParameters, MessageBoxButton.OK, MessageBoxImage.Error
        ));

        return false;
    }

    protected void ShowNoSettingsChanged()
    {
        IsInUpdate = false;
        Dispatcher.Invoke(() => MessageBox.Show(Resources.NoSettingsChanged, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning));
    }
}