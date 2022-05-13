namespace De.Hochstaetter.Fronius.Contracts;

public interface ISwitchable
{
    bool IsSwitchingEnabled { get; }
    bool? IsTurnedOn { get; }
    bool CanSwitch { get; }
    Task TurnOnOff(bool turnOn);
}
