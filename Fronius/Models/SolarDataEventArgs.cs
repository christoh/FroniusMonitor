using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class SolarDataEventArgs:EventArgs
    {
        public SolarDataEventArgs(SolarSystem? solarSystem)
        {
            SolarSystem = solarSystem;
        }

        public SolarSystem? SolarSystem { get; set; }
    }
}
