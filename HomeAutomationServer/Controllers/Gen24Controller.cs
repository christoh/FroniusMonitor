using System.ComponentModel.DataAnnotations;
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

    [HttpGet("{id}/requestStandBy")]
    [BasicAuthorize(Roles = "Operator")]
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
