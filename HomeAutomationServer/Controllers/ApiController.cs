using De.Hochstaetter.Fronius.Models.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[Route("[controller]")]
public class ApiController(ILogger<ApiController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("")]
    [ProducesResponseType(typeof(WebApiInfo), StatusCodes.Status200OK)]
    public IActionResult About()
    {
        logger.LogDebug("Api info was queried by {Ip}", HttpContext.Connection.RemoteIpAddress);

        return Ok(new WebApiInfo
        {
            OsVersion = $"{Environment.OSVersion.VersionString} {nint.Size << 3}-bit",
            OsName = $"{Environment.OSVersion.Platform.ToString()}",
            Manufacturer = "Christoph Hochstätter",
            ProductName = "Home Automation Server",
            Version = new Version("0.5.0.0"),
        });
    }
}
