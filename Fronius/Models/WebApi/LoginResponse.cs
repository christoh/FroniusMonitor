namespace De.Hochstaetter.Fronius.Models.WebApi;

/// <summary>
/// What a login or the renewal of a token answers: who is logged in, and the bearer token every later request
/// carries in its <c>Authorization</c> header.
/// </summary>
public class LoginResponse : UserInfo
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// How long <see cref="AccessToken"/> stays valid, counted from the moment the server answered. A duration
    /// rather than a point in time, so that a client whose clock is wrong still renews the token in time.
    /// </summary>
    public int ExpiresInSeconds { get; set; }
}
