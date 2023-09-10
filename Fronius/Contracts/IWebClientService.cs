using De.Hochstaetter.Fronius.Models.Gen24.Commands;
using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.Fronius.Contracts;

public interface IWebClientService
{
    WebConnection? InverterConnection { get; set; }
    WebConnection? FritzBoxConnection { get; set; }
    Task<Gen24Sensors> GetFroniusData(Gen24Components components);
    ValueTask<T?> SendFroniusCommand<T>(string request, JToken? token = null) where T : Gen24NoResultCommand, new();
    ValueTask FritzBoxLogin();
    ValueTask<FritzBoxDeviceList> GetFritzBoxDevices();
    ValueTask TurnOnFritzBoxDevice(string ain);
    ValueTask TurnOffFritzBoxDevice(string ain);
    ValueTask SetFritzBoxLevel(string ain, double level);
    ValueTask SetFritzBoxColorTemperature(string ain, double temperatureKelvin);
    ValueTask SetFritzBoxColor(string ain, double hueDegrees, double saturation);
    ValueTask<IOrderedEnumerable<Gen24Event>> GetFroniusEvents();
    ValueTask<T> ReadGen24Entity<T>(string request) where T : new();
    ValueTask<(string JsonString, HttpStatusCode StatusCode)> GetFroniusStringResponse(string request, JToken? token = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null);
    ValueTask<(JToken Token, HttpStatusCode StatusCode)> GetFroniusJsonResponse(string request, JToken? token = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null);
    Task<string> GetConfigString(string category, string key);
    Task<string> GetUiString(string category, string key);
    Task<string> GetFroniusName<T>(T enumValue) where T : Enum;
    public Task<string> GetChannelString(string category);
    ValueTask<string> GetEventDescription(string code);
    ValueTask<Gen24StandByStatus?> GetInverterStandByStatus();
    ValueTask RequestInverterStandBy(bool isStandBy);
}