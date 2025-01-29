namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class NewConnectedInverterViewModel : ViewModelBase
{
    public Gen24ConnectedInverter ConnectedInverter
    {
        get;
        set => Set(ref field, value);
    } = new();

    public string HostnameOrIpAddress
    {
        get;
        set => Set(ref field, value);
    } = "192.168.178.1";

    internal override async Task OnInitialize()
    {
        await base.OnInitialize();
        ConnectedInverter.DisplayName = Loc.NewInverter;
    }

    internal void OnOkClicked()
    {
        if (!IPAddress.TryParse(HostnameOrIpAddress, out var ipAddress))
        {
            ConnectedInverter.Hostname = new IdnMapping().GetAscii(HostnameOrIpAddress.Trim());
            ConnectedInverter.IpAddress = null;
        }
        else
        {
            ConnectedInverter.IpAddress = ipAddress;
            ConnectedInverter.Hostname = string.Empty;
        }

        ConnectedInverter.UseDevice = true;
    }
}
