using System.Text.Encodings.Web;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
/// Authenticates a SignalR connection by the ticket it brings along. See <see cref="HubTicketService"/> for why the
/// hub has a scheme of its own instead of taking the Basic credentials of the rest of the API.
/// </summary>
public sealed class HubTicketAuthenticationService(
    HubTicketService tickets,
    IOptionsMonitor<UserList> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "HubTicket";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // WebSockets carry the ticket in the query string, because no header can be set on the handshake. Negotiate
        // and long polling are ordinary HTTP requests, where SignalR sends the same value as a bearer token.
        var ticket = Request.Query["access_token"].FirstOrDefault() ?? BearerToken();

        if (string.IsNullOrEmpty(ticket))
        {
            Logger.LogDebug("A hub connection was attempted without a ticket");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (tickets.Validate(ticket) is not { } user)
        {
            // HubTicketService has already logged which part of the ticket did not hold up.
            return Task.FromResult(AuthenticateResult.Fail("The hub ticket is not valid"));
        }

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Hub connection authenticated as {Username}", user.Username);
        }

        return Task.FromResult(AuthenticateResult.Success(user.CreateAuthenticationTicket(Scheme.Name)));
    }

    private string? BearerToken() => Request.Headers.Authorization.ToString() is { } header && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? header["Bearer ".Length..]
        : null;
}
