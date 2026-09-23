using System.Security.Cryptography;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using HubTicketService = De.Hochstaetter.HomeAutomationServer.Services.HubTicketService;
using Microsoft.AspNetCore.Http;

namespace De.Hochstaetter.HomeAutomationServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController(Settings settings, ILogger<IdentityController> logger, IOptionsMonitor<UserList> userDb) : ControllerBase
{
    // HttpOnly, because no script has any business reading a credential. SameSite=Strict, because the CORS policy
    // lets every origin send requests with credentials: without it, any web page the logged-in browser opens could
    // call the API as that user.
    private static readonly CookieOptions cookieOptions = new() { Path = "/api", HttpOnly = true, SameSite = SameSiteMode.Strict };
    private static readonly byte[] aesKey = IoC.Get<IAesKeyProvider>().GetAesKey();

    [HttpGet("requestKey")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public IActionResult RequestKey([FromQuery] string user)
    {
        var dbUser = FindUser(user);
        var salt = aesKey.Xor(dbUser?.SaltBytes ?? []).Xor((long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays / 10);
        var derived = Rfc2898DeriveBytes.Pbkdf2(user, salt, 32768, HashAlgorithmName.SHA256,16);
        var hashCode = Convert.ToBase64String(derived);
        return Ok(hashCode);
    }

    /// <summary>
    /// Logs in and answers with the bearer token every later request carries, together with the user's name and
    /// roles, so the client can show who is logged in and what they may do without a second round trip. This is
    /// the login the client uses: the password is in the body, where no access log sees it.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Login([FromBody] LoginRequest request, [FromServices] BearerTokenService tokens) => Login(request.UserName, request.Password, tokens);

    /// <summary>
    /// <see cref="Login(LoginRequest, BearerTokenService)"/> for a browser's address bar, which can only send a
    /// GET. It puts the password into the query string, and so into every access log on the way; it is there for
    /// debugging together with <see cref="AuthenticationSettings.EnableCookieAuthentication"/>.
    /// </summary>
    [HttpGet("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Login([FromQuery] string user, [FromQuery] string password, [FromServices] BearerTokenService tokens)
    {
        var dbUser = FindUser(user);

        if (dbUser == null || !dbUser.Authenticate(password))
        {
            Response.Cookies.Delete(ApiAuthenticationService.AuthCookie, cookieOptions);

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Login failed for user {Username}", user);
            }

            return Unauthorized(Helpers.GetProblemDetails(Loc.CannotLogin, Loc.LoginIncorrect));
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Username} logged in successfully from {Ip}", user, HttpContext.Connection.RemoteIpAddress);
        }

        return Ok(IssueToken(dbUser, tokens));
    }

    /// <summary>
    /// A new bearer token for whoever the current one belongs to, which the client asks for shortly before the
    /// current one expires. The current one stays valid until it runs out, so that a request already on its way
    /// is not refused.
    /// </summary>
    [HttpPost("token")]
    [ApiAuthorize]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult RenewToken([FromServices] BearerTokenService tokens)
    {
        var userName = HttpContext.User.Identity?.Name;

        if (FindUser(userName) is not { } dbUser)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("No bearer token for {Username}: authenticated, but not in the user list", userName);
            }

            return Unauthorized(Helpers.GetProblemDetails(Loc.CannotLogin, Loc.LoginIncorrect));
        }

        return Ok(IssueToken(dbUser, tokens));
    }

    /// <summary>
    /// Ends the session: the bearer token the request carries stops working at once, and the cookie is deleted.
    /// Needs no authorization, so that a client which is not sure whether it is still logged in can call it.
    /// </summary>
    [HttpGet("logout")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public IActionResult Logout([FromServices] BearerTokenService tokens)
    {
        var header = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(header))
        {
            header = Request.Cookies[ApiAuthenticationService.AuthCookie] ?? string.Empty;
        }

        if (tokens.Revoke(ApiAuthenticationService.GetCredentials(header, "Bearer")) is { } user && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Username} logged out", user.Username);
        }

        Response.Cookies.Delete(ApiAuthenticationService.AuthCookie, cookieOptions);
        return Ok(true);
    }

    /// <summary>
    /// Hands out a short lived ticket the client authenticates its SignalR connection with. The credentials of the
    /// API get this far, but must not go any further: see <see cref="HubTicketService"/>.
    /// </summary>
    [HttpGet("hubTicket")]
    [ApiAuthorize]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult HubTicket([FromServices] HubTicketService hubTickets)
    {
        var userName = HttpContext.User.Identity?.Name;
        var dbUser = FindUser(userName);

        if (dbUser == null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("No hub ticket for {Username}: authenticated, but not in the user list", userName);
            }

            return Unauthorized(Helpers.GetProblemDetails(Loc.CannotLogin, Loc.LoginIncorrect));
        }

        // Content, not Ok: the ticket is an opaque string and the client reads it as one, without JSON quoting.
        return Content(hubTickets.Issue(dbUser));
    }

    [HttpGet("users")]
    [ApiAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<IEnumerable<UserInfo>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public IActionResult GetUsers()
    {
        return Ok(userDb.CurrentValue.Users.OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase).Select(ToUserInfo));
    }

    [HttpPost("users")]
    [ApiAuthorize(Roles = nameof(Roles.Administrator))]
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
    [ApiAuthorize(Roles = nameof(Roles.Administrator))]
    [ProducesResponseType<UserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUser(string userName, [FromBody] UserAccount account)
    {
        // The new name is logged next to the old one: a rename that silently does nothing looks exactly like a
        // rename that was never asked for, and only the request body tells the two apart.
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation
            (
                "User {ChangedUsername} will be changed to name {NewUsername} and roles {NewRoles} by {Username} from {Ip}",
                userName, account.UserName, account.Roles, HttpContext.User.Identity!.Name, HttpContext.Connection.RemoteIpAddress
            );
        }

        if (userDb.CurrentValue.IsBuiltInGuest(userName))
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotUpdateUser, Loc.GuestCannotBeChanged));
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

    /// <summary>
    /// Lets the logged in user change their own password, without the Administrator role <see cref="UpdateUser"/>
    /// needs. The current password must be given and match, so a hijacked session alone cannot lock the real
    /// user out; there is no username in the route because it is always the caller's own account.
    /// </summary>
    [HttpPut("password")]
    [ApiAuthorize]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userName = HttpContext.User.Identity!.Name!;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Username} is changing their own password from {Ip}", userName, HttpContext.Connection.RemoteIpAddress);
        }

        if (userDb.CurrentValue.IsBuiltInGuest(userName))
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotChangePassword, Loc.GuestCannotBeChanged));
        }

        if (FindUser(userName) is not { } dbUser || !dbUser.Authenticate(request.CurrentPassword))
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotChangePassword, Loc.CurrentPasswordIncorrect));
        }

        dbUser.SetPassword(request.NewPassword);
        await settings.SaveAsync().ConfigureAwait(false);
        return Ok(true);
    }

    [HttpDelete("users/{userName}")]
    [ApiAuthorize(Roles = nameof(Roles.Administrator))]
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

        if (userDb.CurrentValue.IsBuiltInGuest(userName))
        {
            return UnprocessableEntity(Helpers.GetProblemDetails(Loc.CannotDeleteUser, Loc.GuestCannotBeDeleted));
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

    /// <summary>
    /// A new bearer token for <paramref name="user"/>, and - where cookie authentication is switched on - the same
    /// token as the cookie a browser sends back on its own. The cookie lives exactly as long as the token.
    /// </summary>
    private LoginResponse IssueToken(User user, BearerTokenService tokens)
    {
        var token = tokens.Issue(user);
        Response.Cookies.Delete(ApiAuthenticationService.AuthCookie, cookieOptions);

        if (userDb.CurrentValue.Authentication.EnableCookieAuthentication)
        {
            Response.Cookies.Append(ApiAuthenticationService.AuthCookie, "Bearer " + token.Value, new CookieOptions(cookieOptions) { MaxAge = token.Lifetime });
        }

        return new LoginResponse
        {
            UserName = user.Username,
            Roles = user.Roles,
            AccessToken = token.Value,
            ExpiresInSeconds = (int)token.Lifetime.TotalSeconds,
        };
    }

    private User? FindUser(string? userName) => userDb.CurrentValue.Find(userName);

    private bool IsLastAdministrator(User user)
    {
        return user.Roles.HasFlag(Roles.Administrator) && !userDb.CurrentValue.Users.Any(u => u != user && u.Roles.HasFlag(Roles.Administrator));
    }

    private static UserInfo ToUserInfo(User user) => new() { UserName = user.Username, Roles = user.Roles };
}
