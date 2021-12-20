using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using FroniusMonitor;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public MainViewModel(ISolarSystemService solarSystemService)
        {
            SolarSystemService = solarSystemService;
        }

        public ISolarSystemService SolarSystemService { get; }


        public async Task OnInitialize()
        {
            await SolarSystemService.Start(new InverterConnection {BaseUrl = App.Settings.BaseUrl!}).ConfigureAwait(false);
        }
    }
}
