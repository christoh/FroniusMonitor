namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class NewConnectedInverterViewModel : ViewModelBase
{
    private Gen24ConnectedInverter connectedInverter = new();

    public Gen24ConnectedInverter ConnectedInverter
    {
        get => connectedInverter;
        set => Set(ref connectedInverter, value);
    }

    private string hostnameOrIpAddress = "192.168.178.1";

    public string HostnameOrIpAddress
    {
        get => hostnameOrIpAddress;
        set => Set(ref hostnameOrIpAddress, value);
    }

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
