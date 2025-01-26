using De.Hochstaetter.Fronius.Models.Gen24;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
#pragma warning disable CS9107
public class FritzBoxDevicesController(IDataControlService controlService, ILogger<FritzBoxDevicesController> logger) : HomeAutomationControllerBase(controlService)
{
    [HttpGet]
    [ProducesResponseType<IDictionary<string, Gen24System>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetFritzBoxDevices() => GetDevices<FritzBoxDevice>();

    [HttpGet("{id}")]
    [ProducesResponseType<FritzBoxDevice>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetFritzBoxDevice([FromRoute] string id) => GetDevice<FritzBoxDevice>(id);
}
