using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

/// <summary>
///     The Toshiba air conditioners the server currently holds, for a client to load once. Live changes come over the
///     hub as <c>ToshibaHvacMappingDevice</c> messages, and commands go through <c>HomeAutomationHub.SendToshibaHvacCommand</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
#pragma warning disable CS9107
public class ToshibaHvacController(IDataControlService controlService, ILogger<ToshibaHvacController> logger) : DeviceControllerBase(controlService, logger)
{
    [HttpGet]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IDictionary<string, ToshibaHvacMappingDevice>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetToshibaHvacDevices() => GetDevices<ToshibaHvacMappingDevice>();

    [HttpGet("{id}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<ToshibaHvacMappingDevice>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetToshibaHvacDevice([FromRoute] string id) => GetDevice<ToshibaHvacMappingDevice>(id);
}
