namespace De.Hochstaetter.Fronius.Contracts;

public interface IColorTemperatureControl
{
    public bool HasColorTemperatureControl { get; }
    public bool IsColorTemperatureEnabled { get; }
    public bool IsColorTemperatureActive { get; }
    public double? ColorTemperatureKelvin { get; }
    public Task SetColorTemperature(double colorTemperatureKelvin);
    public double MinTemperatureKelvin { get; }
    public double MaxTemperatureKelvin { get; }
}