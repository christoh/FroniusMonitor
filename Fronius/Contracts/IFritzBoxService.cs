namespace De.Hochstaetter.Fronius.Contracts;

public interface IFritzBoxService
{
    WebConnection? Connection { get; set; }
    ValueTask FritzBoxLogin(CancellationToken token = default);
    Task<FritzBoxDeviceList> GetFritzBoxDevices(CancellationToken token = default);
    ValueTask TurnOnFritzBoxDevice(string ain, CancellationToken token = default);
    ValueTask TurnOffFritzBoxDevice(string ain, CancellationToken token = default);
    ValueTask SetFritzBoxLevel(string ain, double level, CancellationToken token = default);
    ValueTask SetFritzBoxColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default);
    ValueTask SetFritzBoxColor(string ain, double hueDegrees, double saturation, CancellationToken token = default);
}
