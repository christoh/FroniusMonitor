using System.Security.Cryptography;
using System.Text;
using De.Hochstaetter.HomeAutomationServer.Misc;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController(Settings settings, ILogger<IdentityController> logger, IOptionsMonitor<UserList> userDb) : ControllerBase
{
    private static readonly CookieOptions cookieOptions = new() { Path = "/api", MaxAge = new TimeSpan(7, 0, 0, 0) };

    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromQuery] string user, [FromQuery] string password)
    {
        var dbUser = userDb.CurrentValue.Users.SingleOrDefault(u => string.Equals(user, u.Username, StringComparison.OrdinalIgnoreCase));

        if (dbUser == null || !dbUser.Authenticate(password))
        {
            Response.Cookies.Delete("auth", cookieOptions);
            logger.LogWarning("Login failed for user {Username}",user);
            return Unauthorized(Helpers.GetValidationDetails(nameof(password), "The username or the password was incorrect"));
        }

        Response.Cookies.Delete("auth", cookieOptions);
        Response.Cookies.Append("auth", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")), cookieOptions);
        logger.LogInformation("{Username} logged in successfully",user);
        return Ok();
    }

    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        if (HttpContext.User.Identity?.Name is {} userName)
        {
            logger.LogInformation("{Username} logged out",userName);
        }
        Response.Cookies.Delete("auth", cookieOptions);
        return Ok();
    }

    [HttpGet("adduser")]
    [BasicAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUser([FromQuery] string user,[FromQuery] string password,[FromQuery] string roles)
    {
        if (userDb.CurrentValue.Users.Select(u => u.Username).Contains(user))
        {
            return BadRequest(Helpers.GetValidationDetails(nameof(user), $"User {user} already exists"));
        }
        
        var split = roles.Split(',');
        var eRoles = Roles.None;

        foreach (var roleString in split)
        {
            if (Enum.TryParse(typeof(Roles), roleString, true, out var result) && result is Roles r)
            {
                eRoles |= r;
            }
            else
            {
                return BadRequest(Helpers.GetValidationDetails(nameof(roles), $"Unknown role '{roleString}'"));
            }
        }
        
        var dbUser = new User { Username = user, Roles = eRoles };
        dbUser.SetPassword(password);
        userDb.CurrentValue.Users.Add(dbUser);
        await settings.SaveAsync().ConfigureAwait(false);
        return Ok(dbUser);
    }
}
