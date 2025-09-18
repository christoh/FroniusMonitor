namespace De.Hochstaetter.Fronius.Contracts;

public interface IFritzBoxService
{
    WebConnection? Connection { get; set; }
    ValueTask FritzBoxLogin(CancellationToken token = default);
    Task<FritzBoxDeviceList> GetDevices(CancellationToken token = default);
    ValueTask SwitchDevice(string ain, bool turnOn, CancellationToken token = default);
    ValueTask SetLevel(string ain, double level, CancellationToken token = default);
    ValueTask SetColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default);
    ValueTask SetColor(string ain, double hueDegrees, double saturation, CancellationToken token = default);
}
