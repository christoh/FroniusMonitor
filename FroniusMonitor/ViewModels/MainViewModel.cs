using System.Windows.Input;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.Views;
using De.Hochstaetter.FroniusMonitor.Wpf.Commands;
using FroniusMonitor;
using Unity.Lifetime;

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

        private object? mainViewContent;

        public object? MainViewContent
        {
            get => mainViewContent;
            set => Set(ref mainViewContent, value);
        }

        private DeviceInfo? deviceInfo;

        public DeviceInfo? DeviceInfo
        {
            get => deviceInfo;
            set => Set(ref deviceInfo, value);
        }

        private object? selectedItem;

        public object? SelectedItem
        {
            get => selectedItem;
            set => Set(ref selectedItem, value, SelectDevice);
        }

        public async Task OnInitialize()
        {
            SolarSystem = await solarSystemService.CreateSolarSystem(new InverterConnection { BaseUrl = "http://192.168.44.10" }).ConfigureAwait(false);
        }



        public void SelectDevice()
        {
            switch (SelectedItem)
            {
                case DeviceInfo device:
                    DeviceInfo = device;

                    switch (device)
                    {
                        case Inverter:
                            MainViewContent = IoC.Get<InverterView>();
                            break;
                        default:
                            MainViewContent = null;
                            break;
                    }

                    break;

                default:
                    MainViewContent = null;
                    break;
            }
        }
    }
}
