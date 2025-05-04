using System.Text.Json;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;

public interface IWebClientService : IDisposable
{
    Task<byte[]> GetKeyForUserName(string userName, CancellationToken token = default);

    void Initialize(string baseUri, string productName, string version);

    Task<ProblemDetails?> Login(string userName, string password, CancellationToken token = default);

    Task<ApiResult<IDictionary<string, DeviceInfo>>> ListDevices(CancellationToken token = default);
    
    Task<ApiResult<bool>> SwitchDevice(string deviceId, bool turnOn, CancellationToken token = default);

    Task<ApiResult<bool>> SetDeviceBrightness(string deviceId, double amount, CancellationToken token = default);

    Task<ApiResult<bool>> SetColorTemperature(string deviceId, double temperatureKelvin, CancellationToken token = default);

    Task<ApiResult<bool>> SetHsv(string deviceId, double? hueDegrees = null, double? saturation = null, double? value = null, CancellationToken token = default);

    Task<ApiResult<IDictionary<string, Gen24System>>> GetGen24Devices(CancellationToken token = default);

    Task<ApiResult<JsonElement>> GetGen24Localization(string deviceId, string iso2LanguageCode, string name, CancellationToken token = default);

    Task<ApiResult<IDictionary<string, FritzBoxDevice>>> GetFritzBoxDevices(CancellationToken token = default);

}
