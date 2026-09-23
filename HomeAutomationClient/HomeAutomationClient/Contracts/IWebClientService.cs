using System.Text.Json;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24.Commands;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.HomeAutomationClient.Contracts;

public interface IWebClientService : IDisposable
{
    #region Identity

    /// <summary>
    /// The AES key this server hands out for a user name; the cached password is encrypted with it. Answers with a
    /// <c>ProblemDetails</c> rather than throwing, because this is the first call the client ever makes to a
    /// server and a wrong address has to reach the user as a sentence, not as a socket exception.
    /// </summary>
    Task<ApiResult<byte[]>> GetKeyForUserName(string userName, CancellationToken token = default);

    void Initialize(string baseUri, string productName, string version);

    /// <summary>
    /// Logs in, in exchange for a bearer token that every later call then carries and that is renewed shortly
    /// before it expires. Where the server refuses the token all the same - after a restart it has forgotten every
    /// token it issued - the service logs in again with this password and repeats the call, so a caller never has
    /// to. The answer says who the server thinks logged in and which <see cref="Roles"/> they hold; the client
    /// shows both and does nothing else with them, because the server checks the roles on every call anyway.
    /// </summary>
    /// <remarks>
    /// A refused password ends the session there was. A server that cannot be reached leaves it as it was.
    /// </remarks>
    Task<ApiResult<UserInfo>> Login(string userName, string password, CancellationToken token = default);

    /// <summary>
    /// A short lived ticket that authenticates a SignalR connection. Requires a successful <see cref="Login"/>
    /// first. Fetch one per connection attempt rather than keeping it: it expires within minutes, and the hub asks
    /// for a fresh one on every reconnect anyway.
    /// </summary>
    Task<string?> GetHubTicket(CancellationToken token = default);

    Task<ApiResult<IDictionary<string, DeviceInfo>>> ListDevices(CancellationToken token = default);

    /// <summary>
    /// Ends the session: asks the server to revoke the bearer token and to delete its cookie, if it set one, and
    /// then drops the token and the password this client holds, so the next call goes out unauthenticated. Needs
    /// no role; a client that is no longer sure it is logged in must still be able to call this.
    /// </summary>
    Task<ApiResult<bool>> Logout(CancellationToken token = default);

    #endregion

    #region Users

    /// <summary>Every user the server has, for the user management dialog. Needs the Administrator role.</summary>
    Task<ApiResult<List<UserInfo>>> GetUsers(CancellationToken token = default);

    /// <summary>Creates a user. The password is mandatory here. Needs the Administrator role.</summary>
    Task<ApiResult<UserInfo>> AddUser(UserAccount account, CancellationToken token = default);

    /// <summary>
    /// Changes the roles of an existing user and, when <see cref="UserAccount.Password"/> is not empty, the
    /// password. <paramref name="userName"/> is the user as they are now; <see cref="UserAccount.UserName"/> may
    /// name them anew - the server accepts a rename since the password hash does not depend on the name. Needs
    /// the Administrator role.
    /// </summary>
    Task<ApiResult<UserInfo>> UpdateUser(string userName, UserAccount account, CancellationToken token = default);

    /// <summary>Removes a user. The server refuses the last administrator. Needs the Administrator role.</summary>
    Task<ApiResult<bool>> DeleteUser(string userName, CancellationToken token = default);

    /// <summary>
    /// Changes the password of whoever is currently logged in. Needs no role beyond being logged in, unlike
    /// <see cref="UpdateUser"/> - the server checks the current password instead of a role, so a hijacked
    /// session alone cannot lock the real user out.
    /// </summary>
    Task<ApiResult<bool>> ChangePassword(ChangePasswordRequest request, CancellationToken token = default);

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

    #region EnergyData

    /// <summary>
    /// The price chart data of today and tomorrow, to load once; changes arrive over the hub as
    /// <c>EnergyChartData</c> messages. 404 where the server collects no energy data.
    /// </summary>
    Task<ApiResult<EnergyChartData>> GetEnergyData(CancellationToken token = default);

    /// <summary>One day of the server's local time, yesterday or older from the server's history.</summary>
    Task<ApiResult<EnergyChartData>> GetEnergyData(DateOnly day, CancellationToken token = default);

    #endregion

    #region SolarWeb

    /// <summary>
    /// One chart of Fronius Solar.web: the <paramref name="view"/> over the <paramref name="interval"/> that contains
    /// <paramref name="date"/>. The server serves it from its cache where it can. 404 where the server has no Solar.web
    /// account, 400 for a Premium view of a day, 503 while Solar.web is not to be asked, 502 when it refused.
    /// </summary>
    Task<ApiResult<SolarWebChart>> GetSolarWebChart(SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default);

    /// <summary>
    /// The firmware of the PV system's components as Solar.web reports it, to load once at start; changes arrive
    /// over the hub as <c>SolarWebFirmwareStatus</c> messages. 404 where the server has no Solar.web account.
    /// </summary>
    Task<ApiResult<SolarWebFirmwareStatus>> GetSolarWebFirmwareStatus(CancellationToken token = default);

    #endregion
}
