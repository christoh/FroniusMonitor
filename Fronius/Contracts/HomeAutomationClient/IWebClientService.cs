using System.Text.Json;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;

public interface IWebClientService : IDisposable
{
    #region Identity

    Task<byte[]> GetKeyForUserName(string userName, CancellationToken token = default);

    void Initialize(string baseUri, string productName, string version);

    Task<ProblemDetails?> Login(string userName, string password, CancellationToken token = default);

    /// <summary>
    /// A short lived ticket that authenticates a SignalR connection. Requires a successful <see cref="Login"/>
    /// first. Fetch one per connection attempt rather than keeping it: it expires within minutes, and the hub asks
    /// for a fresh one on every reconnect anyway.
    /// </summary>
    Task<string?> GetHubTicket(CancellationToken token = default);

    Task<ApiResult<IDictionary<string, DeviceInfo>>> ListDevices(CancellationToken token = default);

    #endregion

    #region IPowerConsumer
    Task<ApiResult<bool>> SwitchDevice(string deviceId, bool turnOn, CancellationToken token = default);

    Task<ApiResult<bool>> SetDeviceBrightness(string deviceId, double amount, CancellationToken token = default);

    Task<ApiResult<bool>> SetColorTemperature(string deviceId, double temperatureKelvin, CancellationToken token = default);

    Task<ApiResult<bool>> SetHsv(string deviceId, double? hueDegrees = null, double? saturation = null, double? value = null, CancellationToken token = default);
    Task<ApiResult<IDictionary<string, FritzBoxDevice>>> GetFritzBoxDevices(CancellationToken token = default);

    #endregion
    
    #region GEN24

    Task<ApiResult<Dictionary<string, Gen24System>>> GetGen24Devices(CancellationToken token = default);

    Task<ApiResult<JsonElement>> GetGen24Localization(string deviceId, string iso2LanguageCode, string name, CancellationToken token = default);

    Task<ApiResult<bool>> RequestGen24StandBy(string deviceId, bool isStandBy, CancellationToken token = default);

    Task<ApiResult<Gen24StandByStatus>> GetGen24StandbyStatus(string deviceId, CancellationToken token = default);

    /// <summary>
    /// Everything the settings dialog of one inverter needs, in one round trip. Fetch this before showing the
    /// dialog. It may go stale while the dialog is open: the server reads the inverter again before it writes, so
    /// what ends up being changed is a delta against the inverter and not against this.
    /// </summary>
    Task<ApiResult<Gen24SettingsSnapshot>> GetGen24Settings(string deviceId, CancellationToken token = default);

    /// <summary>The event log of the inverter, for the event log tab.</summary>
    Task<ApiResult<List<Gen24Event>>> GetGen24Events(string deviceId, CancellationToken token = default);

    /// <summary>
    /// Writes the Modbus settings. The whole object goes over; the server works out what differs from what the
    /// inverter currently holds and sends only that.
    /// </summary>
    /// <returns>True where something was written, false where the inverter already held these settings.</returns>
    Task<ApiResult<bool>> SetGen24ModbusSettings(string deviceId, Gen24ModbusSettings settings, CancellationToken token = default);

    /// <inheritdoc cref="SetGen24ModbusSettings"/>
    Task<ApiResult<bool>> SetGen24BatterySettings(string deviceId, Gen24BatterySettings settings, CancellationToken token = default);

    /// <summary>
    /// Writes the time of use rules. These are not a delta: the inverter takes the whole list or nothing, so
    /// either all of them go or - when nothing changed - none.
    /// </summary>
    /// <returns>True where something was written, false where the inverter already held these rules.</returns>
    Task<ApiResult<bool>> SetGen24TimeOfUse(string deviceId, IEnumerable<Gen24ChargingRule> rules, CancellationToken token = default);

    #endregion

    #region WattPilot

    Task<ApiResult<Dictionary<string, WattPilot>>> GetWattPilots(CancellationToken token = default);

    #endregion
}
