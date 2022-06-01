﻿namespace De.Hochstaetter.FroniusMonitor.ViewModels;

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

    public SettingsViewModel(IWebClientService webClientService, IGen24JsonService gen24Service) : base(webClientService, gen24Service) { }

    private ICommand? okCommand;
    public ICommand OkCommand => okCommand ??= new NoParameterCommand(Ok);

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);
        Settings = (Settings)App.Settings.Clone();
        Settings.FroniusConnection ??= new Settings().FroniusConnection;
        SelectedCulture = Cultures.SingleOrDefault(c => c.Value?.ToUpperInvariant() == Settings.Language?.ToUpperInvariant()) ?? Cultures.First();
    }

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

    private static readonly IEnumerable<string> gen24UserNames = new[] {"customer", "technician", "support"};

    public IEnumerable<string> Gen24UserNames => gen24UserNames;

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
        IoC.Get<MainWindow>().SettingsView.Close();
        Settings.FroniusConnection!.BaseUrl = FixUrl(Settings.FroniusConnection!.BaseUrl);
        Settings.FritzBoxConnection!.BaseUrl = FixUrl(Settings.FritzBoxConnection!.BaseUrl);

        if (Settings.Language?.ToUpperInvariant() != SelectedCulture.Value?.ToUpperInvariant())
        {
            Settings.Language = SelectedCulture.Value;

            MessageBox.Show
            (
                IoC.Get<MainWindow>(),
                "The new language settings require that you restart the program." + Environment.NewLine +
                // ReSharper disable StringLiteralTypo
                "Die neuen Spracheinstellungen erfordern, dass Du das Programm neu startest.",
                // ReSharper restore StringLiteralTypo
                "Info", MessageBoxButton.OK, MessageBoxImage.Information
            );
        }

        App.Settings = Settings;
        IoC.Get<MainViewModel>().NotifyOfPropertyChange(nameof(Settings));
        IoC.Get<IWebClientService>().FritzBoxConnection = Settings.HaveFritzBox ? Settings.FritzBoxConnection : null;
        IoC.Get<IWebClientService>().InverterConnection = Settings.FroniusConnection;
        await Settings.Save().ConfigureAwait(false);

        static string FixUrl(string url)
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }

            while (url[^1..] == "/")
            {
                url = url[..^1];
            }

            return url;
        }
    }
}