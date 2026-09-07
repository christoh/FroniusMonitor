using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
/// Issues and checks the short lived tickets a client authenticates its SignalR connection with.
/// </summary>
/// <remarks>
/// <para>
/// A browser cannot put an <c>Authorization</c> header on a WebSocket handshake, so SignalR passes the credential
/// as the <c>access_token</c> query parameter instead - and a query string is written to the access log of every
/// server and proxy on the way. TLS does not help there. So the credential the hub sees must not be the password.
/// </para>
/// <para>
/// A ticket names exactly one user, is signed with a key only this server knows, expires within
/// <see cref="Lifetime"/>, and stops working the moment that user's password changes. A ticket read out of a log
/// is therefore worth nothing by the time anybody finds it.
/// </para>
/// </remarks>
public sealed class HubTicketService(IAesKeyProvider aesKeyProvider, IOptionsMonitor<UserList> users, TimeProvider clock, ILogger<HubTicketService> logger)
{
    /// <summary>
    /// How long a ticket stays usable.
    /// </summary>
    /// <remarks>
    /// It was two minutes on the assumption that a ticket only has to survive one handshake, because the client
    /// fetches one immediately before it connects and SignalR asks <c>AccessTokenProvider</c> again whenever it
    /// reconnects. That turned out not to hold: the ticket goes on being presented while a connection lives -
    /// every long polling request carries it - so a connection that outlived the ticket lost its authentication
    /// rather than its network.
    /// </remarks>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private const string Version = "v1";

    /// <summary>
    /// A key of its own, derived from the server secret, so that signing tickets cannot interact with anything
    /// else the AES key is used for.
    /// </summary>
    private readonly byte[] signingKey = HMACSHA256.HashData(aesKeyProvider.GetAesKey(), "HomeAutomationHubTicket"u8);

    public string Issue(User user)
    {
        var payload = $"{Version}.{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(user.Username))}.{clock.GetUtcNow().Add(Lifetime).ToUnixTimeSeconds()}";
        var ticket = $"{payload}.{Base64Url.EncodeToString(Sign(payload, user))}";

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Issued a hub ticket for {Username}, valid for {Seconds} seconds", user.Username, Lifetime.TotalSeconds);
        }

        return ticket;
    }

    /// <summary>
    /// The user the ticket was issued for, or <see langword="null"/> where the ticket is malformed, forged,
    /// expired, or belongs to a user who has since been removed or changed their password.
    /// </summary>
    public User? Validate(string? ticket)
    {
        if (string.IsNullOrEmpty(ticket))
        {
            logger.LogDebug("No hub ticket to check");
            return null;
        }

        var parts = ticket.Split('.');

        if (parts.Length != 4 || parts[0] != Version)
        {
            logger.LogWarning("A hub ticket of an unknown shape was rejected");
            return null;
        }

        if (!long.TryParse(parts[2], CultureInfo.InvariantCulture, out var expiry))
        {
            logger.LogWarning("A hub ticket without a readable expiry was rejected");
            return null;
        }

        string userName;
        byte[] signature;

        try
        {
            userName = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[1]));
            signature = Base64Url.DecodeFromChars(parts[3]);
        }
        catch (FormatException)
        {
            logger.LogWarning("A hub ticket that is not valid base64url was rejected");
            return null;
        }

        var user = users.CurrentValue.Users.FirstOrDefault(u => u.Username == userName);

        if (user == null)
        {
            logger.LogWarning("A hub ticket for the unknown user {Username} was rejected", userName);
            return null;
        }

        // The signature is checked before the expiry on purpose: the expiry of a ticket nobody signed says nothing.
        if (!CryptographicOperations.FixedTimeEquals(signature, Sign($"{parts[0]}.{parts[1]}.{parts[2]}", user)))
        {
            logger.LogWarning("A hub ticket for {Username} did not carry a valid signature and was rejected", userName);
            return null;
        }

        if (clock.GetUtcNow().ToUnixTimeSeconds() > expiry)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("The hub ticket of {Username} expired at {Expiry:O}", userName, DateTimeOffset.FromUnixTimeSeconds(expiry));
            }

            return null;
        }

        return user;
    }

    /// <summary>
    /// The password hash and the salt go into the signature without appearing in the ticket, so every outstanding
    /// ticket of a user stops working as soon as that user's password is changed.
    /// </summary>
    private byte[] Sign(string payload, User user) => HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes($"{payload}\n{user.PasswordHash}\n{user.Salt}"));
}
