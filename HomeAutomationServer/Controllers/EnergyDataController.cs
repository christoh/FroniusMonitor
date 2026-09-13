using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

/// <summary>
///     The price chart data for a client to load: today and tomorrow, which the hub also pushes as an
///     <c>EnergyChartData</c> message on every change, and any other day from the history.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EnergyDataController(IEnergyDataService energyData, ILogger<EnergyDataController> logger) : ControllerBase
{
    /// <summary>Today and tomorrow, as the collector last assembled them.</summary>
    [HttpGet]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<EnergyChartData>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetCurrent()
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Current energy data was queried by {Ip}", HttpContext.Connection.RemoteIpAddress);
        }

        if (!energyData.IsEnabled)
        {
            return NotConfigured();
        }

        return energyData.Current is { } current ? Ok(current) : NotFound(Helpers.GetProblemDetails("No data yet", "The energy data has not been collected yet"));
    }

    /// <summary>One local day of the server, <c>yyyy-MM-dd</c>. Yesterday and older come from the history.</summary>
    [HttpGet("{day}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<EnergyChartData>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDay([FromRoute] DateOnly day, CancellationToken token)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Energy data of {Day} was queried by {Ip}", day, HttpContext.Connection.RemoteIpAddress);
        }

        if (!energyData.IsEnabled)
        {
            return NotConfigured();
        }

        return Ok(await energyData.GetDayAsync(day, token).ConfigureAwait(false));
    }

    private NotFoundObjectResult NotConfigured() => NotFound(Helpers.GetProblemDetails("Not configured", "This server collects no energy data. Add an EnergyData element to Settings.xml"));
}
