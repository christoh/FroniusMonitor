namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class SettingsViewModel : SettingsViewModelBase
    {
        public SettingsViewModel(IWebClientService webClientService, IGen24JsonService gen24Service) : base(webClientService, gen24Service) { }

        private ICommand? okCommand;
        public ICommand OkCommand => okCommand ??= new NoParameterCommand(Ok);

        internal override async Task OnInitialize()
        {
            await base.OnInitialize().ConfigureAwait(false);
            Settings = (Settings)App.Settings.Clone();
            Settings.FroniusConnection ??= new Settings().FroniusConnection;
        }

        private Settings settings = null!;

        public Settings Settings
        {
            get => settings;
            set => Set(ref settings, value);
        }

        private static readonly IEnumerable<string> gen24UserNames = new[] { "customer", "technician", "support" };

        public IEnumerable<string> Gen24UserNames => gen24UserNames;

        private async void Ok()
        {
            IoC.Get<MainWindow>().SettingsView.Close();
            Settings.FroniusConnection!.BaseUrl = FixUrl(Settings.FroniusConnection!.BaseUrl);
            Settings.FritzBoxConnection!.BaseUrl = FixUrl(Settings.FritzBoxConnection!.BaseUrl);
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
}
