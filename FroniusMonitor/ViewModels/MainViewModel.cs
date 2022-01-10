using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public MainViewModel(ISolarSystemService solarSystemService)
        {
            SolarSystemService = solarSystemService;
        }

        public ISolarSystemService SolarSystemService { get; }

        private bool includeInverterPower;

        public bool IncludeInverterPower
        {
            get => includeInverterPower;
            set => Set(ref includeInverterPower, value);
        }


        public async Task OnInitialize()
        {
            await SolarSystemService.Start(new WebConnection {BaseUrl = App.Settings.BaseUrlFronius!}, new WebConnection {BaseUrl = App.Settings.BaseUrlFritzBox!}).ConfigureAwait(false);
        }
    }
}
