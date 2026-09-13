using System.Security.Cryptography;
using System.Text;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using HubTicketService = De.Hochstaetter.HomeAutomationServer.Services.HubTicketService;
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
        var derived = Rfc2898DeriveBytes.Pbkdf2(user, salt, 32768, HashAlgorithmName.SHA256,16);
        var hashCode = Convert.ToBase64String(derived);
        return Ok(hashCode);
    }

    /// <summary>
    /// Answers with the user's name and roles, so the client can show who is logged in and what they may do
    /// without a second round trip.
    /// </summary>
    [HttpGet("login")]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
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
        return Ok(new UserInfo { UserName = dbUser.Username, Roles = dbUser.Roles });
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

    /// <summary>
    /// Hands out a short lived ticket the client authenticates its SignalR connection with. Basic credentials get
    /// this far, but must not go any further: see <see cref="HubTicketService"/>.
    /// </summary>
    [HttpGet("hubTicket")]
    [BasicAuthorize]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult HubTicket([FromServices] HubTicketService hubTickets)
    {
        var userName = HttpContext.User.Identity?.Name;
        var dbUser = userDb.CurrentValue.Users.SingleOrDefault(u => string.Equals(userName, u.Username, StringComparison.Ordinal));

        if (dbUser == null)
        {
            logger.LogWarning("No hub ticket for {Username}: authenticated, but not in the user list", userName);
            return Unauthorized(Helpers.GetProblemDetails(Loc.CannotLogin, Loc.LoginIncorrect));
        }

        // Content, not Ok: the ticket is an opaque string and the client reads it as one, without JSON quoting.
        return Content(hubTickets.Issue(dbUser));
    }

    [HttpGet("users")]
    [BasicAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<IEnumerable<UserInfo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public IActionResult GetUsers()
    {
        return Ok(userDb.CurrentValue.Users.OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase).Select(ToUserInfo));
    }

    [HttpPost("users")]
    [BasicAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddUser([FromBody] UserAccount account)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("User {NewUsername} will be added by {Username} from {Ip}", account.UserName, HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress);
        }

        if (string.IsNullOrEmpty(account.Password))
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotAddUser, Loc.PasswordRequired));
        }

        if (FindUser(account.UserName) != null)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("User {NewUsername} already exists", account.UserName);
            }

            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotAddUser, string.Format(Loc.UserAlreadyExists, account.UserName)));
        }

        var dbUser = new User { Username = account.UserName, Roles = account.Roles };
        dbUser.SetPassword(account.Password);
        userDb.CurrentValue.Users.Add(dbUser);
        await settings.SaveAsync().ConfigureAwait(false);
        return Ok(ToUserInfo(dbUser));
    }

    /// <summary>
    /// Changes the name, roles and, if <see cref="UserAccount.Password"/> is not empty, the password. The name in
    /// the route is only the key that finds the user to change - it plays no part in the password hash, which is
    /// salted on its own - so <see cref="UserAccount.UserName"/> may name a different, unused user name to rename
    /// them to it.
    /// </summary>
    [HttpPut("users/{userName}")]
    [BasicAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUser(string userName, [FromBody] UserAccount account)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("User {ChangedUsername} will be changed by {Username} from {Ip}", userName, HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress);
        }

        if (FindUser(userName) is not { } dbUser)
        {
            return NotFound(Helpers.GetProblemDetails(Loc.CannotUpdateUser, string.Format(Loc.UserNotFound, userName)));
        }

        if (!account.Roles.HasFlag(Roles.Administrator) && IsLastAdministrator(dbUser))
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotUpdateUser, Loc.LastAdministrator));
        }

        if (!string.Equals(userName, account.UserName, StringComparison.OrdinalIgnoreCase) && FindUser(account.UserName) != null)
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotUpdateUser, string.Format(Loc.UserAlreadyExists, account.UserName)));
        }

        dbUser.Username = account.UserName;
        dbUser.Roles = account.Roles;

        if (!string.IsNullOrEmpty(account.Password))
        {
            dbUser.SetPassword(account.Password);
        }

        await settings.SaveAsync().ConfigureAwait(false);
        return Ok(ToUserInfo(dbUser));
    }

    [HttpDelete("users/{userName}")]
    [BasicAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteUser(string userName)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("User {DeletedUsername} will be deleted by {Username} from {Ip}", userName, HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress);
        }

        if (FindUser(userName) is not { } dbUser)
        {
            return NotFound(Helpers.GetProblemDetails(Loc.CannotDeleteUser, string.Format(Loc.UserNotFound, userName)));
        }

        if (string.Equals(userName, HttpContext.User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotDeleteUser, Loc.CannotDeleteSelf));
        }

        userDb.CurrentValue.Users.Remove(dbUser);
        await settings.SaveAsync().ConfigureAwait(false);
        return Ok(true);
    }

    private User? FindUser(string userName)
    {
        return userDb.CurrentValue.Users.SingleOrDefault(u => string.Equals(userName, u.Username, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsLastAdministrator(User user)
    {
        return user.Roles.HasFlag(Roles.Administrator) && !userDb.CurrentValue.Users.Any(u => u != user && u.Roles.HasFlag(Roles.Administrator));
    }

    private static UserInfo ToUserInfo(User user) => new() { UserName = user.Username, Roles = user.Roles };
}
