namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// The credentials the server last accepted, kept so the next start can pre-fill the login. Written after a
/// login, and again when an administrator changes their own password: from then on the stored password is the
/// only one the server takes.
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
}