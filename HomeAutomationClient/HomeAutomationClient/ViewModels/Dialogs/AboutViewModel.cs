namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The About box, ported from <c>AboutView</c> of FroniusMonitor: version, build and platform of this app, and below
/// them the same for the server it is connected to, as the server reports them at <c>GET /api</c>. Both are a
/// <see cref="WebApiInfo"/>, so the view shows them with one template.
/// </summary>
/// <param name="server">
/// What the server said, fetched by whoever opens the dialog; null where it did not answer, and then the dialog shows
/// this app alone.
/// </param>
public sealed partial class AboutViewModel(DialogParameters parameters, WebApiInfo? server) : DialogBase<DialogParameters, bool, AboutView>(parameters)
{
    public WebApiInfo Client { get; } = WebApiInfo.ForThisProcess(Loc.AppName);

    public WebApiInfo? Server { get; } = server;

    [RelayCommand]
    private void CloseDialog()
    {
        Result = true;
        Close();
    }

    public override Task AbortAsync()
    {
        CloseDialog();
        return Task.CompletedTask;
    }
}
