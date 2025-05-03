using System.Security.Authentication;
using De.Hochstaetter.Fronius.Models.WebApi;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(IDataControlService controlService, ILogger<DevicesController> logger) : ControllerBase
{
    [HttpGet]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IDictionary<string, DeviceInfo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult ListDevices()
    {
        var result = new Dictionary<string, DeviceInfo>();

        controlService.Entities.Apply(e =>
        {
            var info = new DeviceInfo();
            var type = e.Value.Device.GetType();
            info.Interfaces = type.GetInterfaces().Select(i => i.Name + (i.IsGenericType ? $"${string.Join(',', i.GenericTypeArguments.Select(g => g.Name))}>" : string.Empty)).ToList();
            info.DeviceType = type.Name;
            info.CredentialType = e.Value.Credentials?.GetType().Name;
            info.ServiceType = e.Value.ServiceType?.Name;

            if (e.Value.Device is IHaveDisplayName displayName)
            {
                info.DisplayName = displayName.DisplayName;
            }

            result.Add(e.Key, info);
        });

        logger.LogDebug("Device list requested by {Username} from {Ip}", HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress);
        return result.Count == 0 ? NotFound(Helpers.GetProblemDetails("No devices found", "The Home Automation Server currently has no devices")) : Ok(result);
    }

    [HttpGet("getTypes")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public IActionResult GetTypes()
    {
        logger.LogDebug("Available device types requested by {Username} from {Ip}", HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress);
        return Ok(controlService.Entities.Select(d => d.Value.Device.GetType().Name).Distinct());
    }

    [HttpGet("{id}")]
    [BasicAuthorize(Roles = "User")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetDevice([FromRoute] string id)
    {
        if (controlService.Entities.TryGetValue(id, out var device))
        {
            logger.LogDebug("{Username} requested {DeviceName} from {Ip}", HttpContext.User.Identity!.Name, device.Device.Id, HttpContext.Connection.RemoteIpAddress);
            return Ok(device.Device);
        }

        logger.LogError("{Username} requested {DeviceName} from {Ip} but it was not found", HttpContext.User.Identity!.Name, id, HttpContext.Connection.RemoteIpAddress);
        return NotFound(Helpers.GetProblemDetails(Loc.UnknownDevice, string.Format(Loc.DeviceNotFound, id)));
    }

    [HttpGet("{id}/credentials")]
    [BasicAuthorize(Roles = "Administrator")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetCredentials([FromRoute] string id)
    {
        if (controlService.Entities.TryGetValue(id, out var device))
        {
            logger.LogInformation("Credentials for {DeviceName} requested by {Username} from {Ip}", device.Device.Id, HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress);
            return Ok(device.Credentials);
        }

        logger.LogError("The device {DeviceName} was not found", id);
        return NotFound(Helpers.GetProblemDetails(Loc.UnknownDevice, string.Format(Loc.DeviceNotFound, id)));
    }

    [HttpGet("{id}/switch/{state}")]
    [BasicAuthorize(Roles = "PowerUser")]
    [ProducesResponseType<bool?>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SwitchDevice([FromRoute] string id, [FromRoute] string state)
    {
        bool turnOn;
        bool? previousState;

        try
        {
            turnOn = state.ToLowerInvariant() switch
            {
                "on" => true,
                "off" => false,
                _ => throw new ArgumentException(Loc.CanOnlyTurnOnOrOff),
            };

            if (!controlService.Entities.TryGetValue(id, out var device))
            {
                logger.LogError("The device {DeviceName} was not found", id);
                return NotFound(Helpers.GetProblemDetails(Loc.UnknownDevice, string.Format(Loc.DeviceNotFound, id)));
            }

            var deviceName = device.Device is IHaveDisplayName namedDevice ? namedDevice.DisplayName : id;

            logger.LogInformation
            (
                "{Username} want to switch {DeviceName} to {OnOff} from {Ip}",
                HttpContext.User.Identity!.Name,
                device.Device.Id,
                turnOn ? "on" : "off",
                HttpContext.Connection.RemoteIpAddress
            );

            if (device.Device is not ISwitchable switchableDevice)
            {
                logger.LogError("The device {DeviceName} is not switchable", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceNotSwitchable, deviceName)));
            }

            if (!switchableDevice.IsSwitchingEnabled)
            {
                logger.LogError("Switching is disabled for {DeviceName}", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceSwitchingDisabled, deviceName)));
            }

            previousState = switchableDevice.IsTurnedOn;
            await switchableDevice.TurnOnOff(turnOn).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, ex.Message));
        }

        return Ok(!previousState.HasValue || previousState.Value != turnOn);
    }
}
