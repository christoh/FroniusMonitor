using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationServer.Misc;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

public abstract class HomeAutomationControllerBase(IDataControlService controlService) : ControllerBase
{
    protected IActionResult GetDevices<T>()
    {
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
        return result.Count > 0 ? Ok(result) : NoContent();
    }

    protected IActionResult GetDevice<T>(string id)
    {
        if (controlService.Entities.TryGetValue(id, out var managedDevice) && managedDevice.Device is T device)
        {
            return Ok(device);
        }

        return BadRequest(Helpers.GetValidationDetails(nameof(id), $"A '{typeof(T).Name}' with id '{id}' was not found"));
    }
}
