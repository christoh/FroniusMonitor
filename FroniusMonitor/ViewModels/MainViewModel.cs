using System.Windows.Input;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.Wpf.Commands;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly ISolarSystemService solarSystemService;

        public MainViewModel(ISolarSystemService solarSystemService)
        {
            this.solarSystemService = solarSystemService;
        }

        private SolarSystem? solarSystem;

        public SolarSystem? SolarSystem
        {
            get => solarSystem;
            set => Set(ref solarSystem, value);
        }

        public ICommand SelectDeviceCommand { get; set; } = null!;

        public async Task OnInitialize()
        {
            SelectDeviceCommand = new Command<DeviceInfo>(SelectDevice);
            SolarSystem = await solarSystemService.CreateSolarSystem(new InverterConnection {BaseUrl = "http://192.168.44.10"}).ConfigureAwait(false);
        }

        public void SelectDevice(DeviceInfo? device)
        {
            switch (device)
            {
                case Inverter inverter:
                    //IoC.Get<InverterControl>();
                    break;
                default:
                    break;
            }
        }
    }
}
