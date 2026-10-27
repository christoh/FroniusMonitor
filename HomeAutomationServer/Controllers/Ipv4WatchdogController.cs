using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

/// <summary>
///     The IPv4 watchdog of a FRITZ!Box: what it is doing, its outage log, and a test cycle on demand. Both are for
///     operators, because a test cycle interrupts the box's internet connection twice for about 20 s.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Ipv4WatchdogController(IIpv4WatchdogService watchdog, IIpv4WatchdogStore store, ILogger<Ipv4WatchdogController> logger) : ControllerBase
{
    /// <summary>How many outages and cycles the status carries.</summary>
    private const int RecentCount = 20;

    /// <summary>The watchdog's state, and its newest outages and cycles.</summary>
    [HttpGet]
    [ApiAuthorize(Roles = "Operator")]
    [ProducesResponseType<Ipv4WatchdogStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(CancellationToken token)
    {
        if (!watchdog.IsEnabled)
        {
            return NotConfigured();
        }

        return Ok(watchdog.Status with
        {
            RecentOutages = await store.GetOutagesAsync(RecentCount, token).ConfigureAwait(false),
            RecentCycles = await store.GetCyclesAsync(RecentCount, token).ConfigureAwait(false),
        });
    }

    /// <summary>
    ///     Runs a cycle at the next tick although IPv4 may work: IPv6 off, and back on once the box answers again.
    ///     The hourly limit and the other rules of a cycle apply; <c>GET</c> shows how it went.
    /// </summary>
    [HttpPost("test-cycle")]
    [ApiAuthorize(Roles = "Operator")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult StartTestCycle()
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("A test cycle of the IPv4 watchdog was asked for by {User} from {Ip}", User.Identity?.Name, HttpContext.Connection.RemoteIpAddress);
        }

        return watchdog.RequestTestCycle() switch
        {
            Ipv4WatchdogTestRequest.Accepted => Accepted(),
            Ipv4WatchdogTestRequest.CycleRunning => Conflict(Helpers.GetProblemDetails("Cycle running", "A cycle is running already; GET api/Ipv4Watchdog shows it")),
            _ => NotConfigured(),
        };
    }

    private NotFoundObjectResult NotConfigured() => NotFound(Helpers.GetProblemDetails("Not configured", "This server watches no FRITZ!Box. Add an Ipv4Watchdog element to Settings.xml"));
}
