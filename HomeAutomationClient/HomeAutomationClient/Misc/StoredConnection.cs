namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// The credentials the server last accepted, kept so the next start can pre-fill the login. Written after a
/// login, and again when an administrator changes their own password: from then on the stored password is the
/// only one the server takes. Forgotten at a logout.
/// </summary>
internal static class StoredConnection
{
    /// <returns><see langword="null"/> where the credentials are stored, and what went wrong otherwise.</returns>
    public static async Task<ProblemDetails?> SaveAsync(string userName, string password)
    {
        // Without the key of this server the password would be encrypted with whatever key happens to be current,
        // and would come back as noise on the next start. Nothing is written in that case.
        if (await IoC.GetRegistered<IServerBasedAesKeyProvider>().SetKeyFromUserName(userName) is { } problem)
        {
            return problem;
        }

        var connection = IoC.GetRegistered<HomeAutomationServerConnection>();
        WebConnection.InvalidateKey();
        connection.UserName = userName;
        connection.Password = password;
        connection.BaseUrl = IoC.Get<MainViewModel>().ApiUri;
        await connection.UpdateChecksumAsync();
        await IoC.GetRegistered<ICache>().AddOrUpdateAsync(CacheKeys.Connection, connection);
        return null;
    }

    /// <summary>
    /// Forgets the user name and the password, for a logout: whoever uses this device next must not find them in
    /// the login box, let alone be logged in with them at the next start. The server address is kept - it is not
    /// a credential, and without it the next user would have to know it before they could type anything else.
    /// </summary>
    public static Task ForgetAsync() => IoC.GetRegistered<ICache>().RemoveAsync(CacheKeys.Connection);
}