using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using De.Hochstaetter.Fronius.Models.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public partial class SettingsViewModel(
    IGen24Service gen24Service,
    IGen24JsonService gen24JsonService,
    IFritzBoxService fritzBoxService,
    IWattPilotService wattPilotService,
    IDataCollectionService dataCollectionService
) : SettingsViewModelBase(dataCollectionService, gen24Service, gen24JsonService, fritzBoxService, wattPilotService)
{
    private readonly ElectricityPriceSettings oldElectricityPriceSettings = new();

    private bool isOkPressed;

    [field: AllowNull, MaybeNull]
    public ICommand DeletePanelLayoutFileCommand => field ??= new NoParameterCommand(() => Settings.CustomSolarPanelLayout = null);

    public IEnumerable<ListItemModel<Protocol>> AzureProtocols { get; }= 
    [
        new EnumListItemModel<Protocol> { Value = Protocol.Amqp },
        new EnumListItemModel<Protocol> { Value = Protocol.Mqtt },
    ];

    public IEnumerable<ListItemModel<TunnelMode>> TunnelModes { get; } =
    [
        new EnumListItemModel<TunnelMode> { Value = TunnelMode.Auto },
        new EnumListItemModel<TunnelMode> { Value = TunnelMode.Websocket },
        new EnumListItemModel<TunnelMode> { Value = TunnelMode.NoTunnel },
    ];

    public IEnumerable<ElectricityPriceService> PriceServices { get; } = Enum.GetValues<ElectricityPriceService>();

    public ListItemModel<Protocol> SelectedProtocol
    {
        get => AzureProtocols.FirstOrDefault(p => p.Value == Settings.ToshibaAcConnection.Protocol, new ListItemModel<Protocol>());
        set
        {
            Settings.ToshibaAcConnection.Protocol = value.Value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanUseTunnel));
        }
    }

    public ListItemModel<TunnelMode> SelectedTunnelMode
    {
        get => TunnelModes.FirstOrDefault(p => p.Value == Settings.ToshibaAcConnection.TunnelMode, new ListItemModel<TunnelMode>());
        set
        {
            Settings.ToshibaAcConnection.TunnelMode = value.Value;
            NotifyOfPropertyChange();
        }
    }

    public ListItemModel<AwattarCountry> SelectedPriceRegion
    {
        get => PriceRegions.FirstOrDefault(z => z.Value == Settings.ElectricityPrice.PriceRegion, new EnumListItemModel<AwattarCountry> { Value = PriceService.GetSupportedPriceZones().GetAwaiter().GetResult().FirstOrDefault() });
        set
        {
            PriceService.PriceRegion = Settings.ElectricityPrice.PriceRegion = value.Value;
            NotifyOfPropertyChange();
        }
    }

    public bool CanUseTunnel => Settings.ToshibaAcConnection.CanUseTunnel;

    [ObservableProperty]
    public partial Settings Settings { get; set; } = null!;

    public IEnumerable<ListItemModel<string?>> Cultures { get; } =
    [
        new() { DisplayName = Loc.MatchWindowsLanguage, Value = null },
        new() { DisplayName = GetCultureName("en"), Value = "en" },
        new() { DisplayName = GetCultureName("de"), Value = "de" },
        new() { DisplayName = GetCultureName("de-CH"), Value = "de-CH" },
        new() { DisplayName = GetCultureName("de-LI"), Value = "de-LI" },
    ];

    [ObservableProperty]
    public partial ListItemModel<string?> SelectedCulture { get; set; } = null!;

    [ObservableProperty]
    public partial IElectricityPriceService PriceService { get; set; } = IoC.TryGet<IElectricityPriceService>()!;

    [ObservableProperty,NotifyPropertyChangedFor(nameof(SelectedPriceRegion))]
    public partial IEnumerable<ListItemModel<AwattarCountry>> PriceRegions { get; set; } = [];

    public IEnumerable<string> Gen24UserNames { get; } = ["customer", "technician", "support"];

    public IReadOnlyList<byte> FroniusUpdateRates { get; } = [1, 2, 3, 4, 5, 10, 20, 30, 60];

    internal override async Task OnInitialize()
    {
        oldElectricityPriceSettings.CopyFrom(App.Settings.ElectricityPrice);
        App.Settings.ElectricityPrice.ElectricityPriceServiceChanged += OnElectricityPriceServiceChanged;
        await base.OnInitialize().ConfigureAwait(false);
        Settings = (Settings)App.Settings.Clone();
        SelectedCulture = Cultures.SingleOrDefault(c => c.Value?.ToUpperInvariant() == Settings.Language?.ToUpperInvariant()) ?? Cultures.First();
        NotifyOfPropertyChange(nameof(SelectedProtocol));
        OnElectricityPriceServiceChanged(null, default);
    }

    internal override Task CleanUp()
    {
        if (!isOkPressed)
        {
            App.Settings.ElectricityPrice.CopyFrom(oldElectricityPriceSettings);

            if (PriceService.CanSetPriceRegion)
            {
                PriceService.PriceRegion = oldElectricityPriceSettings.PriceRegion;
            }
        }

        App.Settings.ElectricityPrice.ElectricityPriceServiceChanged -= OnElectricityPriceServiceChanged;
        return base.CleanUp();
    }

    private async void OnElectricityPriceServiceChanged(object? sender, ElectricityPriceService e)
    {
        PriceService = IoC.Get<IElectricityPriceService>();

        PriceRegions = (await PriceService.GetSupportedPriceZones())
            .Select(zone => new EnumListItemModel<AwattarCountry> { Value = zone })
            .OrderBy(zoneModel => zoneModel.DisplayName)
            .ToList()
            ;
    }

    [RelayCommand]
    private void ChangeDriftsFile()
    {
        var dialog = new SaveFileDialog
        {
            CheckFileExists = false,
            CheckPathExists = true,
            DefaultExt = ".xml",
            DereferenceLinks = true,
            FileName = string.IsNullOrWhiteSpace(Settings.DriftFileName) ? "Drifts.xml" : Path.GetFileName(Settings.DriftFileName),
            InitialDirectory = string.IsNullOrWhiteSpace(Settings.DriftFileName) ? null : Path.GetDirectoryName(Settings.DriftFileName),
            OverwritePrompt = false,
            ValidateNames = true,
            Title = Loc.SelectDriftsFile,
            Filter = "Extensible Markup Language (*.xml)|*.xml" + Loc.FilterAllFiles
        };

        var result = dialog.ShowDialog();

        if (result is true)
        {
            Settings.DriftFileName = dialog.FileName;
        }
    }

    [RelayCommand]
    private void ChangeEnergyHistoryFile()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            CheckPathExists = true,
            DefaultExt = ".xml",
            DereferenceLinks = true,
            InitialDirectory = string.IsNullOrWhiteSpace(Settings.EnergyHistoryFileName) ? null : Path.GetDirectoryName(Settings.EnergyHistoryFileName),
            ValidateNames = true,
            Title = Loc.SelectEnergyHistoryFile,
            Filter = "Log files (*.log)|*.log" + Loc.FilterAllFiles
        };

        var result = dialog.ShowDialog();

        if (result is true)
        {
            Settings.EnergyHistoryFileName = dialog.FileName;
        }
    }

    [RelayCommand]
    public async Task ChoosePanelLayoutFile()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            CheckPathExists = true,
            DefaultExt = ".xaml",
            DereferenceLinks = true,
            FileName = string.IsNullOrWhiteSpace(Settings.CustomSolarPanelLayout) ? "Sample solar panel layout.xaml" : Path.GetFileName(Settings.CustomSolarPanelLayout),
            InitialDirectory = string.IsNullOrWhiteSpace(Settings.CustomSolarPanelLayout) ? Path.Combine(AppContext.BaseDirectory, "SolarPanels") : Path.GetDirectoryName(Settings.CustomSolarPanelLayout),
            ValidateNames = true,
            Title = Loc.ChoosePanelLayoutFile,
            Filter = "Extensible Application Markup Language (*.xaml)|*.xaml" + Loc.FilterAllFiles,
        };

        var result = dialog.ShowDialog();

        if (!result.HasValue || !result.Value)
        {
            return;
        }

        await using var stream = dialog.OpenFile();

        try
        {
            _ = XamlReader.Load(stream, true);
        }
        catch (Exception ex)
        {
            ShowBox(ex.Message, $"{Loc.Error}: {ex.GetType().Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Settings.CustomSolarPanelLayout = dialog.FileName;
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

    [RelayCommand]
    private async Task Ok()
    {
        try
        {
            isOkPressed = true;
            Close();
            Settings.FroniusConnection.BaseUrl = FixUrl(Settings.FroniusConnection.BaseUrl);
            Settings.FroniusConnection2.BaseUrl = FixUrl(Settings.FroniusConnection2.BaseUrl);
            Settings.FritzBoxConnection.BaseUrl = FixUrl(Settings.FritzBoxConnection.BaseUrl);
            Settings.WattPilotConnection.BaseUrl = FixUrl(Settings.WattPilotConnection.BaseUrl, true);

            if (Settings.Language?.ToUpperInvariant() != SelectedCulture.Value?.ToUpperInvariant())
            {
                Settings.Language = SelectedCulture.Value;

                ShowBox
                (
                    "The new language settings require that you restart the program." + Environment.NewLine +
                    // ReSharper disable StringLiteralTypo
                    "Die neuen Spracheinstellungen erfordern, dass Du das Programm neu startest.",
                    // ReSharper restore StringLiteralTypo
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information
                );
            }

            FritzBoxService.Connection = Settings is { HaveFritzBox: true, ShowFritzBox: true } ? Settings.FritzBoxConnection : null;
            Gen24Service.Connection = Settings.FroniusConnection;

            if (Settings.HaveTwoInverters)
            {
                DataCollectionService.Gen24Service2 ??= DataCollectionService.Container2.GetRequiredService<IGen24Service>();
            }
            else
            {
                DataCollectionService.Gen24Service2 = null;
            }

            DataCollectionService.FroniusUpdateRate = Settings.FroniusUpdateRate;
            App.Settings.CopyFrom(Settings);
            IoC.Get<MainViewModel>().NotifyOfPropertyChange(nameof(Settings));

            Settings.ShowWattPilot = false;
            await WattPilotService.StopAsync().ConfigureAwait(false);
            await DataCollectionService.HvacService.Stop().ConfigureAwait(false);

            await DataCollectionService.Start
            (
                Settings.FroniusConnection,
                Settings.HaveTwoInverters ? Settings.FroniusConnection2 : null,
                Settings.HaveFritzBox ? Settings.FritzBoxConnection : null,
                Settings.HaveWattPilot ? Settings.WattPilotConnection : null
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {

            ShowBox(ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            await Settings.Save().ConfigureAwait(false);
            App.Settings.NotifySettingsChanged();
            _ = Dispatcher.InvokeAsync(() => IoC.Get<MainWindow>().Activate());
        }

        return;

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
