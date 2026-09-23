namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
/// How clients prove who they are: the <c>Authentication</c> element of <c>Settings.xml</c>.
/// </summary>
/// <remarks>
/// <para>
/// A client logs in once with its user name and password and is handed a bearer token, which every later request
/// carries instead of the password. The tokens live in the memory of the server and nowhere else, so a restart of
/// the server ends every session - the client notices the refusal and logs in again with the password it has.
/// </para>
/// <para>
/// Basic and cookie authentication are for debugging - calling the API from a browser or a tool without a client -
/// and are off unless switched on here. No <c>[DefaultValue]</c> on any of the three: every element is written to
/// the file whatever it says, so that what a server accepts is readable from the file rather than implied by what
/// is missing from it.
/// </para>
/// </remarks>
public class AuthenticationSettings
{
    /// <summary>The lifetime a bearer token has unless <see cref="BearerTokenLifetimeMinutes"/> says otherwise.</summary>
    public const int DefaultBearerTokenLifetimeMinutes = 30;

    /// <summary>
    /// How long a bearer token stays valid after it was issued. The client asks for a new one shortly before the
    /// old one runs out, so this is how long a token read out of a log or a memory dump is worth anything, not how
    /// long a session may last. Anything below one minute is taken as one minute.
    /// </summary>
    public int BearerTokenLifetimeMinutes { get; set; } = DefaultBearerTokenLifetimeMinutes;

    /// <summary><see cref="BearerTokenLifetimeMinutes"/> as the <see cref="TimeSpan"/> the code works with.</summary>
    public TimeSpan BearerTokenLifetime => TimeSpan.FromMinutes(Math.Max(1, BearerTokenLifetimeMinutes));

    /// <summary>
    /// Whether a request may carry the user name and password itself, as an <c>Authorization: Basic</c> header.
    /// A debugging aid: it lets a browser or <c>curl</c> call the API without logging in first, and it sends the
    /// password with every single request.
    /// </summary>
    public bool EnableBasicAuthentication { get; set; }

    /// <summary>
    /// Whether logging in also sets an <c>auth</c> cookie holding the bearer token, which a browser then sends
    /// with every request on its own. A debugging aid: after one <c>api/Identity/login?user=...&amp;password=...</c>
    /// in the address bar, the rest of the API can be called from the same browser.
    /// </summary>
    public bool EnableCookieAuthentication { get; set; }
}
