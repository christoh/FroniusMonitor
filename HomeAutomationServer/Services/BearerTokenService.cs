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
    private readonly ConcurrentDictionary<string, Session> sessions = new(StringComparer.Ordinal);

    /// <inheritdoc cref="AuthenticationSettings.BearerTokenLifetimeMinutes"/>
    public TimeSpan Lifetime => users.CurrentValue.Authentication.BearerTokenLifetime;

    public BearerToken Issue(User user)
    {
        RemoveExpired();

        var lifetime = Lifetime;
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
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

        // Looked up again by name rather than trusted: a deleted user must not stay logged in, and the reference
        // comparison means that a user who was deleted and then created again under the same name does not either.
        // A rename keeps the very same object, so it keeps the token as well.
        if (!ReferenceEquals(users.CurrentValue.Find(session.User.Username), session.User) || session.User.PasswordHash != session.PasswordHash || session.User.Salt != session.Salt)
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
    /// Forgets the tokens that have run out. A token that is never presented again is otherwise never looked at,
    /// and each client leaves one behind every time it renews.
    /// </summary>
    private void RemoveExpired()
    {
        var now = clock.GetUtcNow();

        foreach (var (token, session) in sessions)
        {
            if (session.Expires <= now)
            {
                sessions.TryRemove(token, out _);
            }
        }
    }

    /// <summary>
    /// What a token stands for. The password hash and the salt are those of the moment it was issued, so that
    /// changing the password ends every session of that user.
    /// </summary>
    private sealed record Session(User User, string PasswordHash, string Salt, DateTimeOffset Expires);
}
