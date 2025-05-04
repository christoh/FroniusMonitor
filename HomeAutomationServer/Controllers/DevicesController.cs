using System.ComponentModel.DataAnnotations;
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

    [HttpGet("{id}/setBrightness")]
    [BasicAuthorize(Roles = "PowerUser")]
    [ProducesResponseType<bool?>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetBrightness([FromQuery][Required] double amount, [FromRoute] string id)
    {
        try
        {
            if (amount is < 0 or > 1)
            {
                return ValidationProblem(string.Format(Loc.MustBeBetween, nameof(amount), 0, 1), GetType().Name, (int?)HttpStatusCode.BadRequest, Loc.InvalidParameters);
            }

            var (device, deviceName, notFound) = FindDevice(id);

            if (notFound != null)
            {
                return notFound;
            }

            logger.LogDebug
            (
                "{Username} wants to set brightness on {DeviceName} to {Brightness:P0} from {Ip}",
                HttpContext.User.Identity!.Name,
                id,
                amount,
                HttpContext.Connection.RemoteIpAddress
            );

            if (device.Device is not IDimmable dimmable)
            {
                logger.LogError("The device {DeviceName} is not dimmable", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceNotDimmable, deviceName)));
            }

            if (!dimmable.IsDimmingEnabled)
            {
                logger.LogError("Dimming is disabled for {DeviceName}", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceDimmingDisabled, deviceName)));
            }

            var previousAmount = dimmable.Level;
            await dimmable.SetLevel(amount).ConfigureAwait(false);
            return Ok(!previousAmount.HasValue || Math.Abs(previousAmount.Value - amount) > .00001);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, ex.Message));
        }
    }

    [HttpGet("{id}/setColorTemperature")]
    [BasicAuthorize(Roles = "PowerUser")]
    [ProducesResponseType<bool?>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetColorTemperature([FromQuery][Required] double temperatureKelvin, [FromRoute] string id)
    {
        try
        {
            var (device, deviceName, notFound) = FindDevice(id);

            if (notFound != null)
            {
                return notFound;
            }

            logger.LogDebug
            (
                "{Username} wants to set color temperature on {DeviceName} to {Brightness:P0} from {Ip}",
                HttpContext.User.Identity!.Name,
                id,
                temperatureKelvin,
                HttpContext.Connection.RemoteIpAddress
            );

            if (device.Device is not IColorTemperatureControl temperatureControl)
            {
                logger.LogError("The device {DeviceName} has no support for color temperature", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceHasNoColorTemperature, deviceName)));
            }

            if (!temperatureControl.IsColorTemperatureEnabled)
            {
                logger.LogError("Color temperature is disabled for {DeviceName}", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceColorTemperatureDisabled, deviceName)));
            }

            if (temperatureKelvin < temperatureControl.MinTemperatureKelvin || temperatureKelvin > temperatureControl.MaxTemperatureKelvin)
            {
                return ValidationProblem(string.Format(Loc.MustBeBetween, nameof(temperatureKelvin), temperatureControl.MinTemperatureKelvin, temperatureControl.MaxTemperatureKelvin), GetType().Name, (int?)HttpStatusCode.BadRequest, Loc.InvalidParameters);
            }

            var previousAmount = temperatureControl.ColorTemperatureKelvin;
            await temperatureControl.SetColorTemperature(temperatureKelvin).ConfigureAwait(false);
            return Ok(!previousAmount.HasValue || Math.Abs(previousAmount.Value - temperatureKelvin) > .00001);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, ex.Message));
        }
    }

    [HttpGet("{id}/setHsv")]
    [BasicAuthorize(Roles = "PowerUser")]
    [ProducesResponseType<bool?>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetHsv([FromQuery] double? hueDegrees, [FromQuery] double? saturation, [FromQuery] double? value, [FromRoute] string id)
    {
        try
        {
            var (device, deviceName, notFound) = FindDevice(id);

            if (notFound != null)
            {
                return notFound;
            }

            if (hueDegrees == null && saturation == null && value == null)
            {
                return ValidationProblem
                (
                    $"At least one of '{nameof(hueDegrees)}', '{nameof(saturation)}' or '{nameof(value)}' must be specified",
                    GetType().Name, (int?)HttpStatusCode.BadRequest, "Missing parameters"
                );
            }

            if (device.Device is not IHsvColorControl hsv)
            {
                logger.LogError("The device {DeviceName} is not dimmable", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceNotColorable, deviceName)));
            }

            if (!hsv.IsHsvEnabled)
            {
                logger.LogError("Hsv coloring is disabled for {DeviceName}", id);
                return UnprocessableEntity(Helpers.GetProblemDetails(Loc.Error, string.Format(Loc.DeviceColoringDisabled, deviceName)));
            }

            var oldDegrees = hsv.HueDegrees;
            var oldSaturation = hsv.Saturation;
            var oldValue = hsv.Value;

            hueDegrees ??= oldDegrees ?? 0;
            saturation ??= oldSaturation ?? 1;
            value ??= oldValue ?? 1;

            if (hueDegrees is < 0 or > 360)
            {
                return ValidationProblem(string.Format(Loc.MustBeBetween, nameof(hueDegrees), 0, 360), GetType().Name, (int?)HttpStatusCode.BadRequest, Loc.InvalidParameters);
            }

            if (saturation is < 0 or > 1)
            {
                return ValidationProblem(string.Format(Loc.MustBeBetween, nameof(saturation), 0, 1), GetType().Name, (int?)HttpStatusCode.BadRequest, Loc.InvalidParameters);
            }

            if (value is < 0 or > 1)
            {
                return ValidationProblem(string.Format(Loc.MustBeBetween, nameof(value), 0, 1), GetType().Name, (int?)HttpStatusCode.BadRequest, Loc.InvalidParameters);
            }

            await hsv.SetHsv(hueDegrees.Value, saturation.Value, value.Value).ConfigureAwait(false);
            return Ok(true);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, ex.Message));
        }
    }

    [HttpGet("{id}/switch/{state}")]
    [BasicAuthorize(Roles = "PowerUser")]
    [ProducesResponseType<bool?>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SwitchDevice([FromRoute] string id, [FromRoute] string state)
    {
        try
        {
            var turnOn = state.ToLowerInvariant() switch
            {
                "on" => true,
                "off" => false,
                _ => throw new ArgumentException(Loc.CanOnlyTurnOnOrOff),
            };

            var (device, deviceName, notFound) = FindDevice(id);

            if (notFound != null)
            {
                return notFound;
            }

            logger.LogDebug
            (
                "{Username} wants to switch {DeviceName} to {OnOff} from {Ip}",
                HttpContext.User.Identity!.Name,
                id,
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

            var previousState = switchableDevice.IsTurnedOn;
            await switchableDevice.TurnOnOff(turnOn).ConfigureAwait(false);
            return Ok(!previousState.HasValue || previousState.Value != turnOn);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(ex.GetType().Name, ex.Message));
        }
    }

    private (ManagedDevice Device, string? DeviceName, IActionResult? NotFound) FindDevice(string id)
    {
        if (!controlService.Entities.TryGetValue(id, out var device))
        {
            logger.LogError("The device {DeviceName} was not found", id);
            var notFound = NotFound(Helpers.GetProblemDetails(Loc.UnknownDevice, string.Format(Loc.DeviceNotFound, id)));
            return (device!, id, notFound);
        }

        var deviceName = device.Device is IHaveDisplayName namedDevice ? namedDevice.DisplayName : id;
        return (device, deviceName, null);
    }
}
