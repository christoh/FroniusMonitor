using De.Hochstaetter.Fronius;
using Microsoft.Maui.Controls;

namespace FroniusPhone
{
    public partial class App
    {
        private readonly SettingsBase settings;
        private readonly ISolarSystemService solarSystemService;
        private readonly IAesKeyProvider aesKeyProvider;

        public App(AppShell shell, SettingsBase settings, IAesKeyProvider aesKeyProvider, ISolarSystemService solarSystemService)
        {
            this.settings = settings;
            this.solarSystemService=solarSystemService;
            this.aesKeyProvider= aesKeyProvider;
            InitializeComponent();
            MainPage = shell;
        }

        protected override void OnHandlerChanged()
        {
            try
            {
                IoC.Injector = Handler.MauiContext?.Services;
            }
            finally
            {
                base.OnHandlerChanged();
            }
        }

        protected override async void OnStart()
        {
            try
            {
                WebConnection.Aes.Key = aesKeyProvider.GetAesKey();
                settings.HaveWattPilot = true;
                settings.FroniusUpdateRate = 1;
                solarSystemService.FroniusUpdateRate = settings.FroniusUpdateRate;
                await solarSystemService.Start(settings.FroniusConnection, settings.FritzBoxConnection, settings.WattPilotConnection);
            }
            finally
            {
                base.OnStart();
            }
        }

        protected override void OnSleep()
        {
            try
            {
                #if ANDROID || IOS
                solarSystemService.Stop();
                #endif
            }
            finally
            {
                base.OnSleep();
            }
        }

        protected override async void OnResume()
        {
            base.OnResume();
            #if ANDROID || IOS
            await solarSystemService.Start(settings.FroniusConnection, settings.FritzBoxConnection, settings.WattPilotConnection);
            #endif
        }
    }
}