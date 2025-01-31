using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
#pragma warning disable CS9107
public class FritzBoxDeviceController(IDataControlService controlService, ILogger<FritzBoxDeviceController> logger) : HomeAutomationControllerBase(controlService)
{
    [HttpGet]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IDictionary<string, Gen24System>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetFritzBoxDevices() => GetDevices<FritzBoxDevice>();

    [HttpGet("{id}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<FritzBoxDevice>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetFritzBoxDevice([FromRoute] string id) => GetDevice<FritzBoxDevice>(id);
}
