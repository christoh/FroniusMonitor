using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface ITemperatureSensor
    {
        double? TemperatureCelsius => (TemperatureFahrenheit - 32) / 9 * 5;
        double? TemperatureFahrenheit => TemperatureCelsius * 9 / 5 + 32;
    }
}
