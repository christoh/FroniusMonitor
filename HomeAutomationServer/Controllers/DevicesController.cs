using De.Hochstaetter.Fronius.Models.WebApi;
using De.Hochstaetter.HomeAutomationServer.Misc;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(ILogger<DevicesController> logger, IDataControlService controlService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IDictionary<string, DeviceInfo>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ListDevices()
    {
        var result = new Dictionary<string, DeviceInfo>();
        controlService.Entities.Apply(e =>
        {
            var info = new DeviceInfo();
            var type = e.Value.Device.GetType();
            info.Interfaces = type.GetInterfaces().Select(i => i.Name + (i.IsGenericType ? $"${string.Join(',', i.GenericTypeArguments.Select(g => g.Name))}>" : string.Empty));
            info.DeviceType = type.Name;
            info.CredentialType = e.Value.Credentials?.GetType().Name;
            info.ServiceType = e.Value.ServiceType?.Name;

            if (e.Value.Device is IHaveDisplayName displayName)
            {
                info.DisplayName = displayName.DisplayName;
            }

            result.Add(e.Key, info);
        });

        var x = Thread.CurrentPrincipal;

        return result.Count == 0 ? NoContent() : Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetDevice([FromRoute] string id)
    {
        if (controlService.Entities.TryGetValue(id, out var device))
        {
            return Ok(device.Device);
        }

        return BadRequest(Helpers.GetValidationDetails(nameof(id), $"The device {id} was not found"));
    }

    [HttpGet("{id}/credentials")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetCredentials([FromRoute] string id)
    {
        if (controlService.Entities.TryGetValue(id, out var device))
        {
            return Ok(device.Credentials);
        }

        return BadRequest(Helpers.GetValidationDetails(nameof(id), $"The device {id} was not found"));
    }
}
