namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
/// A light whose color temperature can be set. The defaults say "cannot" - see <see cref="IDimmable"/> for why.
/// </summary>
public interface IColorTemperatureControl
{
    public bool HasColorTemperatureControl => false;
    public bool IsColorTemperatureEnabled => false;
    public bool IsColorTemperatureActive => false;
    public double? ColorTemperatureKelvin => null;
    public Task SetColorTemperature(double colorTemperatureKelvin) => throw new NotSupportedException($"{GetType().Name} has no color temperature control");
    public double MinTemperatureKelvin => 0;
    public double MaxTemperatureKelvin => 0;
}
