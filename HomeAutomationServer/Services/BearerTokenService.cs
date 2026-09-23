using System.Buffers.Text;
using System.Security.Cryptography;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>A bearer token as it was issued: the value the client sends, and how long it stays valid.</summary>
public sealed record BearerToken(string Value, TimeSpan Lifetime);

/// <summary>
/// Issues, checks and revokes the bearer tokens the API is called with.
/// </summary>
/// <remarks>
/// <para>
/// A token is 32 random bytes that mean nothing by themselves. What they stand for is kept in the memory of this
/// service and nowhere else: a server has fewer than ten users, so there is nothing to gain from a database or from
/// signing the tokens. The price is that a restart of the server ends every session, and the client handles that
/// by logging in again with the password it has.
/// </para>
/// <para>
/// A token stays valid until it expires even after the client has been given the next one, so that a request
/// already on its way while the token was renewed is not refused. It stops working earlier when the client logs
/// out, and - like a hub ticket - when its user is deleted or their password is changed.
/// </para>
/// </remarks>
public sealed class BearerTokenService(IOptionsMonitor<UserList> users, TimeProvider clock, ILogger<BearerTokenService> logger)
{
    /// <summary>How long a browser tab ticket may take from being issued to being redeemed.</summary>
    public static readonly TimeSpan TabTicketLifetime = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, Session> sessions = new(StringComparer.Ordinal);

    /// <summary>The browser tab tickets that have been issued and not yet redeemed. See <see cref="IssueTabTicket"/>.</summary>
    private readonly ConcurrentDictionary<string, Session> tabTickets = new(StringComparer.Ordinal);

    /// <inheritdoc cref="AuthenticationSettings.BearerTokenLifetimeMinutes"/>
    public TimeSpan Lifetime => users.CurrentValue.Authentication.BearerTokenLifetime;

    public BearerToken Issue(User user)
    {
        RemoveExpired(sessions);

        var lifetime = Lifetime;
        var value = NewSecret();
        sessions[value] = new Session(user, user.PasswordHash, user.Salt, clock.GetUtcNow() + lifetime);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Issued a bearer token for {Username}, valid for {Minutes} minutes. {Count} tokens are valid now", user.Username, lifetime.TotalMinutes, sessions.Count);
        }

        return new BearerToken(value, lifetime);
    }

    /// <summary>
    /// The user the token was issued for, or <see langword="null"/> where this server never issued it (or issued
    /// it before its last restart), where it has expired or was revoked, or where its user has since been deleted
    /// or has changed their password.
    /// </summary>
    public User? Validate(string? token)
    {
        if (string.IsNullOrEmpty(token) || !sessions.TryGetValue(token, out var session))
        {
            // Not a warning: every client presents a token like that once after each restart of the server.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("An unknown bearer token was rejected. It was revoked, or issued before the server was restarted");
            }

            return null;
        }

        if (clock.GetUtcNow() >= session.Expires)
        {
            sessions.TryRemove(token, out _);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("The bearer token of {Username} expired at {Expiry:O}", session.User.Username, session.Expires);
            }

            return null;
        }

        if (!IsCurrent(session))
        {
            sessions.TryRemove(token, out _);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("The bearer token of {Username} was rejected: the user was deleted or has changed their password since", session.User.Username);
            }

            return null;
        }

        return session.User;
    }

    /// <summary>Ends the session of the token at once.</summary>
    /// <returns>The user the token belonged to, or <see langword="null"/> where it was not valid anyway.</returns>
    public User? Revoke(string? token) => token != null && sessions.TryRemove(token, out var session) ? session.User : null;

    /// <summary>
    /// A ticket that lets a browser tab the client opens become a session of <paramref name="user"/>. A tab cannot
    /// be opened with an <c>Authorization</c> header, so the ticket goes into its address instead, and the server
    /// swaps it for a cookie holding a bearer token (see <see cref="BrowserTabSessions"/>).
    /// </summary>
    /// <remarks>
    /// An address is written to the access log of every server and proxy on the way, and to the browser's history.
    /// So the ticket is not the token: it works once, and only for <see cref="TabTicketLifetime"/>, which a ticket read
    /// out of a log has long outlived.
    /// </remarks>
    public string IssueTabTicket(User user)
    {
        RemoveExpired(tabTickets);

        var value = NewSecret();
        tabTickets[value] = new Session(user, user.PasswordHash, user.Salt, clock.GetUtcNow() + TabTicketLifetime);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Issued a browser tab ticket for {Username}, valid for {Seconds} seconds", user.Username, TabTicketLifetime.TotalSeconds);
        }

        return value;
    }

    /// <summary>
    /// The user the ticket was issued for, and the ticket is used up; <see langword="null"/> where it was never
    /// issued, has been redeemed already or has expired, or where its user was deleted or changed their password
    /// in the meantime.
    /// </summary>
    public User? RedeemTabTicket(string? ticket)
    {
        if (string.IsNullOrEmpty(ticket) || !tabTickets.TryRemove(ticket, out var session) || clock.GetUtcNow() >= session.Expires || !IsCurrent(session))
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("A browser tab ticket was rejected: unknown, used already, expired, or its user has changed");
            }

            return null;
        }

        return session.User;
    }

    /// <summary>
    /// Whether the user of <paramref name="session"/> is still the one it was issued for. Looked up again by name
    /// rather than trusted: a deleted user must not stay logged in, and the reference comparison means that a user
    /// who was deleted and then created again under the same name does not either. A rename keeps the very same
    /// object, so it keeps the session as well.
    /// </summary>
    private bool IsCurrent(Session session) =>
        ReferenceEquals(users.CurrentValue.Find(session.User.Username), session.User) && session.User.PasswordHash == session.PasswordHash && session.User.Salt == session.Salt;

    private static string NewSecret() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Forgets what has run out. A token that is never presented again is otherwise never looked at, and each
    /// client leaves one behind every time it renews; a ticket nobody redeemed stays behind the same way.
    /// </summary>
    private void RemoveExpired(ConcurrentDictionary<string, Session> store)
    {
        var now = clock.GetUtcNow();

        foreach (var (key, session) in store)
        {
            if (session.Expires <= now)
            {
                store.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// What a token or a ticket stands for. The password hash and the salt are those of the moment it was issued, so that
    /// changing the password ends every session of that user.
    /// </summary>
    private sealed record Session(User User, string PasswordHash, string Salt, DateTimeOffset Expires);
}
