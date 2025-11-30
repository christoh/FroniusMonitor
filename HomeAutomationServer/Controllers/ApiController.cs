using De.Hochstaetter.Fronius.Models.WebApi;
using De.Hochstaetter.HomeAutomationServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Runtime.InteropServices;

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
            OsVersion = $"{RuntimeInformation.OSDescription}",
            OsArch = $"{RuntimeInformation.OSArchitecture}",
            DotNetVersion = $"{RuntimeInformation.FrameworkDescription}",
            Manufacturer = "Christoph Hochstätter",
            ProductName = $"Home Automation Server {RuntimeInformation.ProcessArchitecture}",
            Version = GitInfo.VersionString,
        });
    }
}
