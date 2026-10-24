namespace De.Hochstaetter.Fronius.Contracts;

public interface IFritzBoxService
{
    /// <summary>
    /// A command has reached the box. The box shows its effect a moment later, so whoever polls it can read again
    /// soon instead of waiting out the interval.
    /// </summary>
    event EventHandler? CommandSent;

    WebConnection? Connection { get; set; }
    ValueTask FritzBoxLogin(CancellationToken token = default);
    Task<FritzBoxDeviceList> GetDevices(CancellationToken token = default);
    ValueTask SwitchDevice(string ain, bool turnOn, CancellationToken token = default);
    ValueTask SetLevel(string ain, double level, CancellationToken token = default);
    ValueTask SetColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default);
    ValueTask SetColor(string ain, double hueDegrees, double saturation, CancellationToken token = default);
}
