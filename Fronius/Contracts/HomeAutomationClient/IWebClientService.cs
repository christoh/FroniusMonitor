using System.Text.Json;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;

public interface IWebClientService : IDisposable
{
    #region Identity

    Task<byte[]> GetKeyForUserName(string userName, CancellationToken token = default);

    void Initialize(string baseUri, string productName, string version);

    /// <summary>
    /// Logs in with Basic credentials, which every later call then carries. The answer says who the server thinks
    /// logged in and which <see cref="Roles"/> they hold; the client shows both and does nothing else with them,
    /// because the server checks the roles on every call anyway.
    /// </summary>
    Task<ApiResult<UserInfo>> Login(string userName, string password, CancellationToken token = default);

    /// <summary>
    /// A short lived ticket that authenticates a SignalR connection. Requires a successful <see cref="Login"/>
    /// first. Fetch one per connection attempt rather than keeping it: it expires within minutes, and the hub asks
    /// for a fresh one on every reconnect anyway.
    /// </summary>
    Task<string?> GetHubTicket(CancellationToken token = default);

    Task<ApiResult<IDictionary<string, DeviceInfo>>> ListDevices(CancellationToken token = default);

    #endregion

    #region Users

    /// <summary>Every user the server has, for the user management dialog. Needs the Administrator role.</summary>
    Task<ApiResult<List<UserInfo>>> GetUsers(CancellationToken token = default);

    /// <summary>Creates a user. The password is mandatory here. Needs the Administrator role.</summary>
    Task<ApiResult<UserInfo>> AddUser(UserAccount account, CancellationToken token = default);

    /// <summary>
    /// Changes the roles of an existing user and, when <see cref="UserAccount.Password"/> is not empty, the
    /// password. A user cannot be renamed. Needs the Administrator role.
    /// </summary>
    Task<ApiResult<UserInfo>> UpdateUser(UserAccount account, CancellationToken token = default);

    /// <summary>Removes a user. The server refuses the last administrator. Needs the Administrator role.</summary>
    Task<ApiResult<bool>> DeleteUser(string userName, CancellationToken token = default);

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

    /// <summary>
    /// Writes the name of the system, its time zone and whether it keeps its clock in step. The whole
    /// <see cref="Gen24InverterSettings"/> goes over and only these three are looked at: they are what lives at
    /// <c>api/config/common</c>, and the string trackers and export limits it carries have endpoints of their own.
    /// </summary>
    /// <inheritdoc cref="SetGen24ModbusSettings" path="/returns"/>
    Task<ApiResult<bool>> SetGen24CommonSettings(string deviceId, Gen24InverterSettings settings, CancellationToken token = default);

    /// <summary>
    /// Writes the settings of the string trackers - power mode, dynamic peak manager, peak power and the fixed
    /// voltage. A tracker the inverter does not have is not invented.
    /// </summary>
    /// <inheritdoc cref="SetGen24ModbusSettings" path="/returns"/>
    Task<ApiResult<bool>> SetGen24MpptSettings(string deviceId, Gen24Mppt mppt, CancellationToken token = default);

    /// <summary>Writes the export limits, and the peak power the visualization refers them to.</summary>
    /// <inheritdoc cref="SetGen24ModbusSettings" path="/returns"/>
    Task<ApiResult<bool>> SetGen24PowerLimits(string deviceId, Gen24PowerLimitSettings powerLimits, CancellationToken token = default);

    #endregion

    #region WattPilot

    Task<ApiResult<Dictionary<string, WattPilot>>> GetWattPilots(CancellationToken token = default);

    #endregion

    #region ToshibaHvac

    /// <summary>The air conditioners the server holds, by the id the hub publishes them under.</summary>
    Task<ApiResult<Dictionary<string, ToshibaHvacMappingDevice>>> GetToshibaHvacDevices(CancellationToken token = default);

    #endregion
}
