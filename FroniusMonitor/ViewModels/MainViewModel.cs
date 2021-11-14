using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly ISolarSystemService solarSystemService;

        public MainViewModel(ISolarSystemService solarSystemServcice)
        {
            this.solarSystemService = solarSystemServcice;
        }

        private SolarSystem? solarSystem;
        public SolarSystem? SolarSystem
        {
            get => solarSystem;
            set => Set(ref solarSystem, value);
        }

        public async Task OnInitialize()
        {
            SolarSystem = await solarSystemService.CreateSolarSystem(new InverterConnection{BaseUrl="http://192.168.44.10"}).ConfigureAwait(false);
        }
    }
}
