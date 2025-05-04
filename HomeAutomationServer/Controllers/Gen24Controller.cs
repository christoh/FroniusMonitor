using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Gen24SystemController(IDataControlService controlService, ILogger<Gen24SystemController> logger) : DeviceControllerBase(controlService, logger)
{
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
        var managedDevice = ControlService.Entities.FirstOrDefault(e => e.Key == id && e.Value.Device is Gen24System);

        if (managedDevice.Value == null)
        {
            return NotFound(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceNotFound, id)));
        }

        if (managedDevice.Value.Credentials is not WebConnection connection)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, $"The device {managedDevice.Value.Device.Model} with serial number {managedDevice.Value.Device.SerialNumber} has no web connection"));
        }

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(connection.BaseUrl);

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
}
