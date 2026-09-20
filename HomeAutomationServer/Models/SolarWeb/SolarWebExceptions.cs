namespace De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;

/// <summary>
///     Solar.web is not to be asked for a while: it answered <c>503</c>, or its maintenance page ("Maintenance
///     Work" / "Wartungsarbeiten", which Fronius puts up on Sundays now and then). <see cref="RetryAfter" /> is
///     what its <c>Retry-After</c> header said, or <see langword="null" /> where it said nothing.
/// </summary>
public class SolarWebUnavailableException(TimeSpan? retryAfter, string? message = null) : Exception(message ?? "Solar.web is not available")
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>Solar.web answered <c>429 Too Many Requests</c>: the same as being unavailable, only that the caller is the reason.</summary>
public sealed class SolarWebRateLimitException(TimeSpan? retryAfter, string? message = null) : SolarWebUnavailableException(retryAfter, message ?? "Solar.web answered 429 Too Many Requests");

/// <summary>
///     The login at <c>login.fronius.com</c> did not end in a Solar.web session: the user name or the password was
///     rejected, or there is no password to send. Not tried again by itself - a rejected password tried over and
///     over brings up a captcha, and then nothing logs in until a human solves it.
/// </summary>
public sealed class SolarWebLoginException(string message, Exception? inner = null) : Exception(message, inner);
