using System.Security.Cryptography;
using System.Text;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController(Settings settings, ILogger<IdentityController> logger, IOptionsMonitor<UserList> userDb) : ControllerBase
{
    private static readonly CookieOptions cookieOptions = new() { Path = "/api", MaxAge = new TimeSpan(7, 0, 0, 0) };
    private static readonly byte[] aesKey = IoC.Get<IAesKeyProvider>().GetAesKey();

    [HttpGet("requestKey")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public IActionResult RequestKey([FromQuery] string user)
    {
        var dbUser = userDb.CurrentValue.Users.SingleOrDefault(u => string.Equals(user, u.Username, StringComparison.OrdinalIgnoreCase));
        var salt = aesKey.Xor(dbUser?.SaltBytes ?? []).Xor((long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays / 10);
        using var derived = new Rfc2898DeriveBytes(user, salt, 32768, HashAlgorithmName.SHA256);
        var hashCode = Convert.ToBase64String(derived.GetBytes(16));
        return Ok(hashCode);
    }

    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Login([FromQuery] string user, [FromQuery] string password)
    {
        var dbUser = userDb.CurrentValue.Users.SingleOrDefault(u => string.Equals(user, u.Username, StringComparison.OrdinalIgnoreCase));

        if (dbUser == null || !dbUser.Authenticate(password))
        {
            Response.Cookies.Delete("auth", cookieOptions);
            logger.LogWarning("Login failed for user {Username}", user);
            return Unauthorized(Helpers.GetProblemDetails(Loc.CannotLogin, Loc.LoginIncorrect));
        }

        Response.Cookies.Delete("auth", cookieOptions);
        Response.Cookies.Append("auth", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")), cookieOptions);
        logger.LogInformation("{Username} logged in successfully from {Ip}", user, HttpContext.Connection.RemoteIpAddress);
        return Ok();
    }

    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        if (HttpContext.User.Identity?.Name is { } userName)
        {
            logger.LogInformation("{Username} logged out", userName);
        }

        Response.Cookies.Delete("auth", cookieOptions);
        return Ok();
    }

    [HttpGet("adduser")]
    [BasicAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddUser([FromQuery] string user, [FromQuery] string password, [FromQuery] string roles)
    {
        logger.LogInformation("User {NewUsername} will be added by {Username} from {Ip}", user, HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress);

        if (userDb.CurrentValue.Users.Select(u => u.Username).Contains(user))
        {
            logger.LogError("User {NewUsername} already exists", user);
            return UnprocessableEntity(Helpers.GetProblemDetails("Cannot add user", $"User {user} already exists"));
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
                logger.LogError("Unknown role '{Role}'", roleString);
                return UnprocessableEntity(Helpers.GetProblemDetails("Cannot add user", $"Unknown role '{roleString}'"));
            }
        }

        var dbUser = new User { Username = user, Roles = eRoles };
        dbUser.SetPassword(password);
        userDb.CurrentValue.Users.Add(dbUser);
        await settings.SaveAsync().ConfigureAwait(false);
        return Ok(dbUser);
    }
}
