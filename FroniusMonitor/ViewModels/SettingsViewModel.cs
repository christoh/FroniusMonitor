using De.Hochstaetter.Fronius.Models.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class SettingsViewModel : SettingsViewModelBase
{
    private static readonly IEnumerable<ListItemModel<string?>> cultures = new[]
    {
        new ListItemModel<string?> {DisplayName = Resources.MatchWindowsLanguage, Value = null},
        new ListItemModel<string?> {DisplayName = GetCultureName("en"), Value = "en"},
        new ListItemModel<string?> {DisplayName = GetCultureName("de"), Value = "de"},
        new ListItemModel<string?> {DisplayName = GetCultureName("de-CH"), Value = "de-CH"},
        new ListItemModel<string?> {DisplayName = GetCultureName("de-LI"), Value = "de-LI"},
    };

    private static readonly IEnumerable<ListItemModel<Protocol>> azureProtocols = new[]
    {
        new EnumListItemModel<Protocol> {Value = Protocol.Amqp},
        new EnumListItemModel<Protocol> {Value = Protocol.Mqtt},
    };

    private static readonly IEnumerable<ListItemModel<TunnelMode>> tunnelModes = new[]
    {
        new EnumListItemModel<TunnelMode> {Value = TunnelMode.Auto},
        new EnumListItemModel<TunnelMode> {Value = TunnelMode.Websocket},
        new EnumListItemModel<TunnelMode> {Value = TunnelMode.NoTunnel},
    };

    public SettingsViewModel
    (
        IWebClientService webClientService,
        IGen24JsonService gen24Service,
        IWattPilotService wattPilotService,
        IDataCollectionService dataCollectionService
    ) : base(dataCollectionService, webClientService, gen24Service, wattPilotService)
    {
    }

    private ICommand? okCommand;
    public ICommand OkCommand => okCommand ??= new NoParameterCommand(Ok);

    private ICommand? changeDriftsFileCommand;
    public ICommand ChangeDriftsFileCommand => changeDriftsFileCommand ??= new NoParameterCommand(ChangeDriftsFile);

    public IEnumerable<ListItemModel<Protocol>> AzureProtocols => azureProtocols;

    public IEnumerable<ListItemModel<TunnelMode>> TunnelModes => tunnelModes;
    
    public ListItemModel<Protocol> SelectedProtocol
    {
        get => AzureProtocols.First(p => p.Value == Settings.ToshibaAcConnection!.Protocol);
        set
        {
            Settings.ToshibaAcConnection!.Protocol = value.Value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanUseTunnel));
        }
    }

    public ListItemModel<TunnelMode> SelectedTunnelMode
    {
        get => TunnelModes.First(p => p.Value == Settings.ToshibaAcConnection!.TunnelMode);
        set
        {
            Settings.ToshibaAcConnection!.TunnelMode = value.Value;
            NotifyOfPropertyChange();
        }
    }

    public bool CanUseTunnel => Settings.ToshibaAcConnection!.CanUseTunnel;

    private Settings settings = null!;

    public Settings Settings
    {
        get => settings;
        set => Set(ref settings, value);
    }

    public IEnumerable<ListItemModel<string?>> Cultures => cultures;

    private ListItemModel<string?> selectedCulture = null!;

    public ListItemModel<string?> SelectedCulture
    {
        get => selectedCulture;
        set => Set(ref selectedCulture, value);
    }

    private static readonly IEnumerable<string> gen24UserNames = new[] { "customer", "technician", "support" };

    public IEnumerable<string> Gen24UserNames => gen24UserNames;

    private static readonly IReadOnlyList<byte> froniusUpdateRates = new byte[] { 1, 2, 3, 4, 5, 10, 20, 30, 60 };

    public IReadOnlyList<byte> FroniusUpdateRates => froniusUpdateRates;

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        Settings = (Settings)App.Settings.Clone();
        Settings.FroniusConnection ??= new Settings().FroniusConnection;
        Settings.ToshibaAcConnection ??= new Settings().ToshibaAcConnection;
        SelectedCulture = Cultures.SingleOrDefault(c => c.Value?.ToUpperInvariant() == Settings.Language?.ToUpperInvariant()) ?? Cultures.First();
        NotifyOfPropertyChange(nameof(SelectedProtocol));
    }

    private void ChangeDriftsFile()
    {
        var dialog = new SaveFileDialog
        {
            CheckFileExists = false,
            CheckPathExists = true,
            DefaultExt = ".xml",
            DereferenceLinks = true,
            FileName = "Drifts.xml",
            InitialDirectory = string.IsNullOrWhiteSpace(Settings.DriftFileName) ? null : Path.GetDirectoryName(Settings.DriftFileName),
            OverwritePrompt = false,
            ValidateNames = true,
            Title = Resources.SelectDriftsFile,
        };

        var result = dialog.ShowDialog();

        if (result.HasValue && result.Value)
        {
            Settings.DriftFileName = dialog.FileName;
        }
    }

    private static string GetCultureName(string id)
    {
        var culture = new CultureInfo(id);

        var set = new HashSet<string>
        {
            culture.NativeName,
            culture.DisplayName,
            culture.EnglishName,
        };

        return string.Join(" / ", set);
    }

    private async void Ok()
    {
        View.Close();
        Settings.FroniusConnection!.BaseUrl = FixUrl(Settings.FroniusConnection!.BaseUrl);
        Settings.FritzBoxConnection!.BaseUrl = FixUrl(Settings.FritzBoxConnection!.BaseUrl);
        Settings.WattPilotConnection!.BaseUrl = FixUrl(Settings.WattPilotConnection!.BaseUrl, true);

        if (Settings.Language?.ToUpperInvariant() != SelectedCulture.Value?.ToUpperInvariant())
        {
            Settings.Language = SelectedCulture.Value;

            Show
            (
                "The new language settings require that you restart the program." + Environment.NewLine +
                // ReSharper disable StringLiteralTypo
                "Die neuen Spracheinstellungen erfordern, dass Du das Programm neu startest.",
                // ReSharper restore StringLiteralTypo
                "Info", MessageBoxButton.OK, MessageBoxImage.Information
            );
        }

        IoC.Get<MainViewModel>().NotifyOfPropertyChange(nameof(Settings));
        WebClientService.FritzBoxConnection = Settings is { HaveFritzBox: true, ShowFritzBox: true } ? Settings.FritzBoxConnection : null;
        WebClientService.InverterConnection = Settings.FroniusConnection;

        if (Settings.HaveTwoInverters)
        {
            DataCollectionService.WebClientService2 ??= IoC.Injector!.CreateScope().ServiceProvider.GetRequiredService<IWebClientService>();
            DataCollectionService.WebClientService2.FritzBoxConnection = WebClientService.FritzBoxConnection;
            DataCollectionService.WebClientService2.InverterConnection = Settings.FroniusConnection2;
        }

        DataCollectionService.FroniusUpdateRate = Settings.FroniusUpdateRate;
        App.Settings.CopyFrom(Settings);

        if (!Settings.HaveWattPilot || !Settings.ShowWattPilot)
        {
            Settings.ShowWattPilot = false;
            DataCollectionService.WattPilotConnection = null;
        }
        else
        {
            DataCollectionService.WattPilotConnection = Settings.WattPilotConnection;
        }

        await DataCollectionService.HvacService.Stop().ConfigureAwait(false);
        await Settings.Save().ConfigureAwait(false);

        static string FixUrl(string url, bool isWebSocket = false)
        {
            if (!url.StartsWith(isWebSocket ? "ws://" : "http://") && !url.StartsWith(isWebSocket ? "wss://" : "https://"))
            {
                url = (isWebSocket ? "ws://" : "http://") + url;
            }

            while (url[^1..] == "/" && url[^3..] != ":")
            {
                url = url[..^1];
            }

            return url;
        }
    }
}
