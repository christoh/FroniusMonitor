namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     Where <see cref="IToshibaHvacService" /> keeps the bearer token of the Toshiba account between runs.
/// </summary>
/// <remarks>
///     The Toshiba service is sensitive to repeated logins with user name and password, and the token it hands out
///     lasts for months. So the token is persisted by whoever owns the settings - the WPF app in its own
///     <c>Settings.xml</c>, the server in its - and the service only logs in again when the stored token is rejected.
/// </remarks>
public interface IToshibaHvacSessionStore
{
    /// <summary>The stored session, or <see langword="null" /> when no login has succeeded yet.</summary>
    ToshibaHvacSession? Session { get; }

    /// <summary>Persists a session the service just obtained by logging in.</summary>
    Task SaveSessionAsync(ToshibaHvacSession session);
}
