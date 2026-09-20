namespace De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;

/// <summary>
///     Solar.web answered <c>429 Too Many Requests</c>. <see cref="RetryAfter" /> is what its <c>Retry-After</c>
///     header said, or <see langword="null" /> where it said nothing.
/// </summary>
public sealed class SolarWebRateLimitException(TimeSpan? retryAfter, string? message = null) : Exception(message ?? "Solar.web answered 429 Too Many Requests")
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>
///     The login at <c>login.fronius.com</c> did not end in a Solar.web session: the user name or the password was
///     rejected, or a page turned up that the login flow does not know. Not tried again by itself - a rejected
///     password tried over and over brings up a captcha, and then nothing logs in until a human solves it.
/// </summary>
public sealed class SolarWebLoginException(string message, Exception? inner = null) : Exception(message, inner);
