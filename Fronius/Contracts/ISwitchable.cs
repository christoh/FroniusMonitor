using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts;

public interface ISwitchable:IHaveDisplayName
{
    bool IsPresent { get; }
    bool IsEnabled { get; }
    bool? IsTurnedOn { get; }
    string? Model { get; }
    bool CanTurnOnOff { get; }
    Task TurnOnOff(bool turnOn);
}