using De.Hochstaetter.Fronius.Models.Gen24.Commands;

namespace De.Hochstaetter.Fronius.Contracts;

public interface IWebClientService
{
    WebConnection? InverterConnection { get; set; }
    WebConnection? FritzBoxConnection { get; set; }
    Task<Gen24Sensors> GetFroniusData(Gen24Components components, CancellationToken token = default);
    ValueTask<T?> SendFroniusCommand<T>(string request, JToken? jToken = null, CancellationToken token = default) where T : Gen24NoResultCommand, new();
    ValueTask FritzBoxLogin(CancellationToken token = default);
    ValueTask<FritzBoxDeviceList> GetFritzBoxDevices(CancellationToken token = default);
    ValueTask TurnOnFritzBoxDevice(string ain, CancellationToken token = default);
    ValueTask TurnOffFritzBoxDevice(string ain, CancellationToken token = default);
    ValueTask SetFritzBoxLevel(string ain, double level, CancellationToken token = default);
    ValueTask SetFritzBoxColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default);
    ValueTask SetFritzBoxColor(string ain, double hueDegrees, double saturation, CancellationToken token = default);
    ValueTask<IOrderedEnumerable<Gen24Event>> GetFroniusEvents(CancellationToken token = default);
    ValueTask<T> ReadGen24Entity<T>(string request, CancellationToken token = default) where T : new();
    ValueTask<(string JsonString, HttpStatusCode StatusCode)> GetFroniusStringResponse(string request, JToken? jToken = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default);
    ValueTask<(JToken Token, HttpStatusCode StatusCode)> GetFroniusJsonResponse(string request, JToken? jToken = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default);
    Task<string> GetConfigString(string path, CancellationToken token = default);
    Task<string> GetUiString(string path, CancellationToken token = default);
    Task<string> GetFroniusName<T>(T enumValue, CancellationToken token = default) where T : Enum;
    public Task<string> GetChannelString(string category, CancellationToken token = default);
    ValueTask<string> GetEventDescription(string code, CancellationToken token = default);
    ValueTask<Gen24StandByStatus?> GetInverterStandByStatus(CancellationToken token = default);
    ValueTask RequestInverterStandBy(bool isStandBy, CancellationToken token = default);
}