using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class InverterViewModel : BindableBase
    {
        private Inverter inverter = null!;

        public Inverter Inverter
        {
            get => inverter;
            set => Set(ref inverter, value);
        }

        public async Task OnInitialize()
        {
            await Task.Run(() => { });
        }
    }
}
