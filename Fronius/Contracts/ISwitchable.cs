using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts;

public interface ISwitchable
{
    bool IsEnabled { get; }
    bool? IsTurnedOn { get; }
    bool CanSwitch { get; }
    Task TurnOnOff(bool turnOn);
}