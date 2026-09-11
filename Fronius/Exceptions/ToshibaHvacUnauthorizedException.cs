namespace De.Hochstaetter.Fronius.Exceptions;

/// <summary>
///     The Toshiba service answered with HTTP 401 or 403: the bearer token it was given is no longer accepted. This
///     is the one failure that justifies a new login with user name and password.
/// </summary>
public class ToshibaHvacUnauthorizedException(string uri, HttpStatusCode statusCode)
    : UnauthorizedAccessException($"The Toshiba service refused the bearer token for {uri} with HTTP {(int)statusCode} ({statusCode})")
{
    public string Uri { get; } = uri;
    public HttpStatusCode StatusCode { get; } = statusCode;
}
