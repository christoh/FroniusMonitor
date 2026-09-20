using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

/// <summary>
///     Solar.web for a client: the charts, <c>GET api/SolarWeb/{interval}/{view}/{date}</c>, for instance
///     <c>api/SolarWeb/day/production/2026-09-19</c> or <c>api/SolarWeb/month/expense/2026-09-01</c> (without a
///     date it is today's period), and the firmware status of the system's components, <c>GET api/SolarWeb/firmware</c>,
///     which a client asks at start and gets pushed by the hub afterwards. The server caches and paces; see
///     <see cref="ISolarWebService" />.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SolarWebController(ISolarWebService solarWeb, ILogger<SolarWebController> logger) : ControllerBase
{
    [HttpGet("{interval}/{view}/{day?}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<SolarWebChart>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> GetChart([FromRoute] SolarWebInterval interval, [FromRoute] SolarWebView view, [FromRoute] DateOnly? day, CancellationToken token)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("The Solar.web {View} chart of {Interval} {Day} was queried by {Ip}", view, interval, day, HttpContext.Connection.RemoteIpAddress);
        }

        return AnswerAsync(() => solarWeb.GetChartAsync(interval, view, day ?? solarWeb.Today, token), token);
    }

    /// <summary>The firmware of every component as Solar.web last reported it, read again where the last report is older than the refresh interval.</summary>
    [HttpGet("firmware")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<SolarWebFirmwareStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> GetFirmwareStatus(CancellationToken token)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("The Solar.web firmware status was queried by {Ip}", HttpContext.Connection.RemoteIpAddress);
        }

        return AnswerAsync(() => solarWeb.GetFirmwareStatusAsync(token), token);
    }

    /// <summary>Runs one request to the service and turns each way Solar.web can refuse into its status code.</summary>
    private async Task<IActionResult> AnswerAsync<T>(Func<Task<T>> request, CancellationToken token)
    {
        if (!solarWeb.IsEnabled)
        {
            return NotFound(Helpers.GetProblemDetails("Not configured", "This server reads no Solar.web data. Add a SolarWeb element with a user name, a password and a PvSystemId to Settings.xml"));
        }

        try
        {
            return Ok(await request().ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Helpers.GetProblemDetails("No such chart", ex.Message));
        }
        catch (SolarWebUnavailableException ex)
        {
            if (ex.RetryAfter is { } retryAfter)
            {
                Response.Headers.RetryAfter = Math.Max(1, (long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, Helpers.GetProblemDetails(ex is SolarWebRateLimitException ? "Solar.web rate limit" : "Solar.web not available", ex.Message));
        }
        catch (SolarWebLoginException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, Helpers.GetProblemDetails("Solar.web login failed", ex.Message));
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, Helpers.GetProblemDetails("Solar.web did not answer", ex.Message));
        }
        catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
        {
            // The client's timeout, not the caller going away.
            return StatusCode(StatusCodes.Status504GatewayTimeout, Helpers.GetProblemDetails("Solar.web did not answer in time", ex.Message));
        }
    }
}
