using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Runtime.InteropServices;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[Route("[controller]")]
public class ApiController(ILogger<ApiController> logger) : ControllerBase
{
    /// <summary>
    /// The server's version, build and platform. Anonymous, so that it also answers whoever only wants to know
    /// whether a server is there and which one; the About dialog of the client shows it below its own.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("")]
    [ProducesResponseType(typeof(WebApiInfo), StatusCodes.Status200OK)]
    public IActionResult About()
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Api info was queried by {Ip}", HttpContext.Connection.RemoteIpAddress);
        }

        return Ok(WebApiInfo.ForThisProcess($"Home Automation Server {RuntimeInformation.ProcessArchitecture}"));
    }
}
