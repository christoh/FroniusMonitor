namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IPowerConsumer1P : IPowerMeter1P, ISwitchable, ITemperatureSensor, IDimmable, IHsvColorControl, IColorTemperatureControl
    {
        new string? Model { get; }
    }
}
