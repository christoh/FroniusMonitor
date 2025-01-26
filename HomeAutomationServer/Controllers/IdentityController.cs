using System.Security.Claims;
using System.Security.Principal;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController(ILogger<IdentityController> logger, IOptions<UserList> userDb) : ControllerBase
{
    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromQuery] string user, [FromQuery] string password)
    {
        var dbUser = userDb.Value.Users.SingleOrDefault(u => string.Equals(user, u.Username, StringComparison.OrdinalIgnoreCase));

        if (dbUser == null || !dbUser.Authenticate(password))
        {
            return Unauthorized();
        }

        var principal = new Principal
        {
            Auth =
            {
                Name = user,
                IsAuthenticated = true
            },
            Roles = dbUser.Roles
        };

        
        Thread.CurrentPrincipal = principal;
        return Ok();
    }
}
