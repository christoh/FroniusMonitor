using De.Hochstaetter.Fronius;

namespace FroniusPhone
{
    public partial class App
    {
        private readonly SettingsBase settings;
        private readonly ISolarSystemService solarSystemService;
        private readonly IAesKeyProvider aesKeyProvider;

        public App(AppShell shell, SettingsBase settings, IAesKeyProvider aesKeyProvider, ISolarSystemService solarSystemService)
        {
            MainPage = shell;
            this.settings = settings;
            this.solarSystemService=solarSystemService;
            this.aesKeyProvider= aesKeyProvider;
            InitializeComponent();
        }

        protected override async void OnStart()
        {
            try
            {
                IoC.Injector = Handler.MauiContext?.Services;
                WebConnection.Aes.Key = aesKeyProvider.GetAesKey();
                settings.HaveWattPilot = true;
                settings.FroniusUpdateRate = 5;
                solarSystemService.FroniusUpdateRate = settings.FroniusUpdateRate;
                await solarSystemService.Start(settings.FroniusConnection, settings.FritzBoxConnection, settings.WattPilotConnection);
            }
            finally
            {
                base.OnStart();
            }
        }
    }
}