using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Models.Gen24.Commands;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;
using System.ComponentModel.DataAnnotations;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Gen24SystemController
(
    IDataControlService controlService,
    IGen24JsonService jsonService,
    IGen24ConfigRefresher configRefresher,
    ILogger<Gen24SystemController> logger
) : DeviceControllerBase(controlService, logger)
{
    /// <summary>
    /// The user name the inverter gives its owner. Everything the inverter reserves for a technician - the string
    /// trackers, the export limits - is refused under this login, so a client is told not to offer it.
    /// </summary>
    private const string CustomerLogin = "customer";

    [HttpGet]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IDictionary<string, Gen24System>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetInverters() => GetDevices<Gen24System>();

    [HttpGet("{id}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IDictionary<string, Gen24System>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetInverter([FromRoute] string id) => GetDevice<Gen24System>(id);

    [HttpGet("{id}/requestStandBy")]
    [BasicAuthorize(Roles = "Operator")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestStandBy([FromRoute] string id, [FromQuery, Required] bool isStandBy)
    {
        try
        {
            var (errorResponse, gen24Service) = GetManagedGen24System(id);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            await gen24Service!.RequestInverterStandBy(isStandBy).ConfigureAwait(false);
            return Ok(true);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, $"The request for stand-by mode on inverter {id} failed: {ex.Message}"));
        }
    }

    [HttpGet("{id}/getStandByStatus")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<Gen24StandByStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStandbyStatus([FromRoute]string id)
    {
        try
        {
            var (errorResponse, gen24Service) = GetManagedGen24System(id);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            var status = await gen24Service!.GetInverterStandByStatus().ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, $"The request for stand-by mode on inverter {id} failed: {ex.Message}"));
        }
    }


    [HttpGet("{id}/i18n/{iso2LanguageCode}/{name}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IDictionary<string, object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetInverterLocalization([FromRoute] string id, [FromRoute] string iso2LanguageCode, [FromRoute] string name)
    {
        iso2LanguageCode = iso2LanguageCode.ToLowerInvariant();
        name = name.ToLowerInvariant();
        var (errorResponse, gen24Service) = GetManagedGen24System(id);

        if (errorResponse != null)
        {
            return errorResponse;
        }

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(gen24Service!.Connection!.BaseUrl);

        try
        {
            var query = name == "events"
                ? $"app/assets/i18n/StateCodeTranslations/{iso2LanguageCode}.json"
                // ReSharper disable once StringLiteralTypo
                : $"app/assets/i18n/WeblateTranslations/{name}/{iso2LanguageCode}.json";

            var stream = await httpClient.GetStreamAsync(query).ConfigureAwait(false);
            return File(stream, "application/json");
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, $"The localization file {httpClient.BaseAddress.AbsoluteUri}/{name}/{iso2LanguageCode}.json could not be downloaded from the inverter: {ex.Message}"));
        }
    }

    /// <summary>
    /// Everything the settings dialog of a client needs, read from the inverter in one go. Call this before showing
    /// the dialog; the write endpoints below read the inverter again themselves, so this may go stale.
    /// </summary>
    [HttpGet("{id}/settings")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<Gen24SettingsSnapshot>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetSettings([FromRoute] string id)
    {
        var (errorResponse, gen24Service) = GetManagedGen24System(id);

        if (errorResponse != null)
        {
            return errorResponse;
        }

        try
        {
            var configToken = await ReadConfig(gen24Service!).ConfigureAwait(false);
            var versionToken = (await gen24Service!.GetFroniusJsonResponse("api/status/version", token: HttpContext.RequestAborted).ConfigureAwait(false)).Token;

            return Ok(new Gen24SettingsSnapshot
            {
                InverterSettings = Gen24InverterSettings.Parse(configToken),
                BatterySettings = Gen24BatterySettings.Parse(configToken["batteries"]?["batteries"]),
                ChargingRules = Gen24ChargingRule.ParseList(configToken["timeofuse"]),
                ModbusSettings = Gen24ModbusSettings.Parse(configToken["modbus"]?["modbus"]),
                SoftwareVersions = Gen24Versions.Parse(versionToken).SwVersions,
                MaxAcPower = configToken["powerunit"]?["powerunit"]?["system"]?["DEVICE_POWERACTIVE_NOMINAL_F32"].AsDouble(),
                IsInverterTechnician = !string.Equals(gen24Service.Connection?.UserName, CustomerLogin, StringComparison.OrdinalIgnoreCase),
            });
        }
        catch (Exception ex)
        {
            return InverterFailed(id, "read the settings of", ex);
        }
    }

    /// <summary>The event log of the inverter, newest first, for the event log tab of the settings dialog.</summary>
    [HttpGet("{id}/events")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IEnumerable<Gen24Event>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetEvents([FromRoute] string id)
    {
        var (errorResponse, gen24Service) = GetManagedGen24System(id);

        if (errorResponse != null)
        {
            return errorResponse;
        }

        try
        {
            var events = await gen24Service!.GetFroniusEvents(HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(events.ToList());
        }
        catch (Exception ex)
        {
            return InverterFailed(id, "read the event log of", ex);
        }
    }

    [HttpPut("{id}/settings/modbus")]
    [BasicAuthorize(Roles = "Operator")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> SetModbusSettings([FromRoute] string id, [FromBody] Gen24ModbusSettings settings) => WriteSettings
    (
        id, "api/config/modbus", settings,
        configToken => Gen24ModbusSettings.Parse(configToken["modbus"]?["modbus"]),
        (wanted, current) => wanted.GetToken(current)
    );

    [HttpPut("{id}/settings/batteries")]
    [BasicAuthorize(Roles = "Operator")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> SetBatterySettings([FromRoute] string id, [FromBody] Gen24BatterySettings settings) => WriteSettings
    (
        id, "api/config/batteries", settings,
        configToken => Gen24BatterySettings.Parse(configToken["batteries"]?["batteries"]),
        (wanted, current) => jsonService.GetUpdateToken(wanted, current)
    );

    /// <summary>
    /// The time of use rules. Unlike the other groups these are not written as a delta: the inverter takes the whole
    /// list or nothing, so either everything goes or - when the list is unchanged - nothing does.
    /// </summary>
    [HttpPut("{id}/settings/timeOfUse")]
    [BasicAuthorize(Roles = "Operator")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> SetTimeOfUse([FromRoute] string id, [FromBody] List<Gen24ChargingRule> rules) => WriteSettings
    (
        id, "api/config/timeofuse", rules,
        configToken => Gen24ChargingRule.ParseList(configToken["timeofuse"]),
        (wanted, current) => wanted.SequenceEqual(current) ? new JsonObject() : Gen24ChargingRule.GetToken(wanted)
    );

    /// <summary>
    /// The name of the system, its time zone and its clock. Everything else the settings object carries has an
    /// endpoint of its own, and the delta of this one only ever covers what lives at <c>api/config/common</c>:
    /// the string trackers and the power limits hang off properties that carry no channel name, so
    /// <see cref="IGen24JsonService.GetUpdateToken{T}"/> does not look at them.
    /// </summary>
    [HttpPut("{id}/settings/common")]
    [BasicAuthorize(Roles = "Operator")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> SetCommonSettings([FromRoute] string id, [FromBody] Gen24InverterSettings settings) => WriteSettings
    (
        id, "api/config/common", settings,
        Gen24InverterSettings.Parse,
        (wanted, current) => jsonService.GetUpdateToken(wanted, current)
    );

    /// <summary>The string trackers, at <c>api/config/powerunit</c>.</summary>
    [HttpPut("{id}/settings/mppt")]
    [BasicAuthorize(Roles = "Operator")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> SetMpptSettings([FromRoute] string id, [FromBody] Gen24Mppt mppt) => WriteSettings
    (
        id, "api/config/powerunit", mppt,
        configToken => Gen24Mppt.Parse(configToken["powerunit"]?["powerunit"]?["mppt"]),
        (wanted, current) => wanted.GetToken(current)
    );

    /// <summary>The export limits, at <c>api/config/limit_settings/powerLimits</c>.</summary>
    [HttpPut("{id}/settings/powerLimits")]
    [BasicAuthorize(Roles = "Operator")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> SetPowerLimits([FromRoute] string id, [FromBody] Gen24PowerLimitSettings powerLimits) => WriteSettings
    (
        id, "api/config/limit_settings/powerLimits", powerLimits,
        configToken => Gen24PowerLimitSettings.Parse(configToken["limit_settings"]?["powerLimits"]),
        (wanted, current) => wanted.GetToken(current)
    );

    /// <summary>
    /// Writes one group of settings to the inverter, and only what actually differs.
    /// </summary>
    /// <remarks>
    /// The current settings are read from the inverter here rather than taken from the caller. A client dialog may
    /// have been open for a while, and a delta against what it saw back then would quietly undo whatever changed
    /// since. This also keeps raw JSON out of the API: the caller sends the settings as an object and never a path
    /// and a payload of its own choosing.
    /// </remarks>
    /// <returns>True where something was written, false where the inverter already held what was asked for.</returns>
    private async Task<IActionResult> WriteSettings<T>
    (
        string id,
        string inverterPath,
        T wanted,
        Func<JsonNode, T> parseCurrent,
        Func<T, T, JsonNode> buildDelta
    )
    {
        var (errorResponse, gen24Service) = GetManagedGen24System(id);

        if (errorResponse != null)
        {
            return errorResponse;
        }

        JsonNode updateToken;

        try
        {
            updateToken = buildDelta(wanted, parseCurrent(await ReadConfig(gen24Service!).ConfigureAwait(false)));
        }
        catch (Exception ex)
        {
            return InverterFailed(id, "read the settings of", ex);
        }

        if (!updateToken.HasValues())
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Inverter {Id} already holds the settings that {Username} asked for at {Path}", id, HttpContext.User.Identity?.Name, inverterPath);
            }

            return Ok(false);
        }

        try
        {
            var (_, status) = await gen24Service!
                .GetFroniusJsonResponse(inverterPath, updateToken, [HttpStatusCode.OK, HttpStatusCode.BadRequest], HttpContext.RequestAborted)
                .ConfigureAwait(false);

            if (status != HttpStatusCode.OK)
            {
                logger.LogWarning("Inverter {Id} refused the settings for {Path}: {Token}", id, inverterPath, updateToken.ToJsonString());
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, $"The inverter refused the settings for {inverterPath}."));
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{Username} changed {Path} on inverter {Id}", HttpContext.User.Identity?.Name, inverterPath, id);
            }

            // The collector polls the configuration every few minutes, so without this the change we have just
            // made would not reach any client until then - and a dialog that reopens in the meantime would show
            // what the inverter held before it.
            configRefresher.ReadConfigNow(id);

            return Ok(true);
        }
        catch (Exception ex)
        {
            return InverterFailed(id, "write the settings of", ex);
        }
    }

    private async Task<JsonNode> ReadConfig(IGen24Service gen24Service) =>
        (await gen24Service.GetFroniusJsonResponse("api/config/", token: HttpContext.RequestAborted).ConfigureAwait(false)).Token;

    private IActionResult InverterFailed(string id, string what, Exception ex)
    {
        logger.LogError(ex, "Could not {What} inverter {Id}", what, id);
        return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, $"Could not {what} inverter {id}: {ex.Message}"));
    }

    private (IActionResult? ErrorResponse, IGen24Service? Gen24Service) GetManagedGen24System(string id)
    {
        if (!ControlService.Entities.TryGetValue(id, out var managedDevice) || managedDevice.Device is not Gen24System)
        {
            return (NotFound(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceNotFound, id))), null);
        }

        if (managedDevice.Credentials is not WebConnection connection || managedDevice.ServiceType is null || IoC.Get(managedDevice.ServiceType) is not IGen24Service gen24Service)
        {
            return (UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, $"The device {managedDevice.Device.Model} with serial number {managedDevice.Device.SerialNumber} has no web connection")), null);
        }

        gen24Service.Connection = connection;
        return (null, gen24Service);
    }
}
