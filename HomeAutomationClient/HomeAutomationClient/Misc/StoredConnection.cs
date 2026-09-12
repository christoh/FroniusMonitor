namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// The credentials the server last accepted, kept so the next start can pre-fill the login. Written after a
/// login, and again when an administrator changes their own password: from then on the stored password is the
/// only one the server takes.
/// </summary>
internal static class StoredConnection
{
    public static async Task SaveAsync(string userName, string password)
    {
        await IoC.GetRegistered<IServerBasedAesKeyProvider>().SetKeyFromUserName(userName);
        var connection = IoC.GetRegistered<HomeAutomationServerConnection>();
        WebConnection.InvalidateKey();
        connection.UserName = userName;
        connection.Password = password;
        connection.BaseUrl = IoC.Get<MainViewModel>().ApiUri;
        await connection.UpdateChecksumAsync();
        await IoC.GetRegistered<ICache>().AddOrUpdateAsync(CacheKeys.Connection, connection);
    }
}