namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IPowerConsumer1P : IHaveDisplayName, IPowerMeter1P, ISwitchable, ITemperatureSensor, IDimmable
    {
        bool IsPresent { get; }
        string? Model { get; }
    }
}
