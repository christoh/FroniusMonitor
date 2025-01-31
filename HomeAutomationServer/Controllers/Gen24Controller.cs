using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Gen24SystemController(ILogger<Gen24SystemController> logger, IDataControlService controlService) : HomeAutomationControllerBase(controlService)
{
    [HttpGet]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IDictionary<string, Gen24System>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetInverters() => GetDevices<Gen24System>();

    [HttpGet("{id}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<Gen24System>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetInverter([FromRoute] string id) => GetDevice<Gen24System>(id);
}
