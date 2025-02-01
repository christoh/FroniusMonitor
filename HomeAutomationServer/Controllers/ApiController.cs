using De.Hochstaetter.Fronius.Models.WebApi;
using Microsoft.AspNetCore.Authorization;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[Route("[controller]")]
public class ApiController(ILogger<ApiController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("")]
    public IActionResult About()
    {
        logger.LogDebug("Api info was queried by {Ip}", HttpContext.Connection.RemoteIpAddress);
        
        return Ok(new WebApiInfo
        {
            OsVersion = $"{Environment.OSVersion.VersionString} {(Environment.Is64BitOperatingSystem ? "64" : "32")}-bit",
            OsName = $"{Environment.OSVersion.Platform.ToString()}",
            Manufacturer = "Christoph Hochstätter",
            ProductName = "Home Automation Server",
            Version = new Version("0.9.0.0"),
        });
    }
}
