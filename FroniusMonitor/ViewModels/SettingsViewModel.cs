using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class SettingsViewModel : SettingsViewModelBase
    {
        public SettingsViewModel(IWebClientService webClientService, IGen24JsonService gen24Service) : base(webClientService, gen24Service) { }

        internal override async Task OnInitialize()
        {
            await base.OnInitialize().ConfigureAwait(false);
            Settings = (Settings)App.Settings.Clone();

            if (Settings.FroniusConnection == null)
            {
                Settings.FroniusConnection = new Settings().FroniusConnection;
            }
        }

        private Settings settings = null!;

        public Settings Settings
        {
            get => settings;
            set => Set(ref settings, value);
        }

        private static readonly IEnumerable<string> gen24UserNames = new[] { "customer", "technician", "support" };

        public IEnumerable<string> Gen24UserNames => gen24UserNames;
    }
}
