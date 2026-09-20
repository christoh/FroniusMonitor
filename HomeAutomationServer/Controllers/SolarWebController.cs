using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

/// <summary>
///     Solar.web's charts for a client to draw: <c>GET api/SolarWeb/{interval}/{view}/{date}</c>, for instance
///     <c>api/SolarWeb/day/production/2026-09-19</c> or <c>api/SolarWeb/month/expense/2026-09-01</c>. Without a
///     date it is today's period. The server caches and paces; see <see cref="ISolarWebService" />.
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
    public async Task<IActionResult> GetChart([FromRoute] SolarWebInterval interval, [FromRoute] SolarWebView view, [FromRoute] DateOnly? day, CancellationToken token)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("The Solar.web {View} chart of {Interval} {Day} was queried by {Ip}", view, interval, day, HttpContext.Connection.RemoteIpAddress);
        }

        if (!solarWeb.IsEnabled)
        {
            return NotFound(Helpers.GetProblemDetails("Not configured", "This server reads no Solar.web data. Add a SolarWeb element with a user name, a password and a PvSystemId to Settings.xml"));
        }

        try
        {
            return Ok(await solarWeb.GetChartAsync(interval, view, day ?? solarWeb.Today, token).ConfigureAwait(false));
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
