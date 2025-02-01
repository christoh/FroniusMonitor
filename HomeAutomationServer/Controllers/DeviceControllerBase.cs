using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationServer.Misc;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

public abstract class DeviceControllerBase(IDataControlService controlService, ILogger logger) : ControllerBase
{
    protected IActionResult GetDevices<T>()
    {
        logger.LogDebug("All devices of type {DeviceType} were queried by {Ip}", typeof(T).Name, HttpContext.Connection.RemoteIpAddress);

        var list = controlService.Entities.Where(e => e.Value.Device is T).Select(e =>
        {
            var device = (T)e.Value.Device;

            var result = new KeyValuePair<string, T>
            (
                e.Key,
                device
            );

            return result;
        });

        var result = new Dictionary<string, T>(list);

        if (result.Count > 0)
        {
            return Ok(result);
        }

        logger.LogError("No devices of type {DeviceType} were found", typeof(T).Name);
        return NoContent();
    }

    protected IActionResult GetDevice<T>(string id)
    {
        if (controlService.Entities.TryGetValue(id, out var managedDevice) && managedDevice.Device is T device)
        {
            logger.LogDebug("Device {DeviceName} was queried by {Ip}", managedDevice.Device.Id, HttpContext.Connection.RemoteIpAddress);
            return Ok(device);
        }

        logger.LogError("A '{DeviceType}' with id '{Id}' was not found", typeof(T).Name, id);
        return BadRequest(Helpers.GetValidationDetails(nameof(id), $"A '{typeof(T).Name}' with id '{id}' was not found"));
    }
}
