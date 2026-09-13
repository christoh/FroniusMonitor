using System.Security.Authentication;

namespace De.Hochstaetter.HomeAutomationClient.Crypto;

public class AesKeyProvider(IWebClientService webClient) : IServerBasedAesKeyProvider
{
    private byte[]? aesKey;

    public byte[] GetAesKey() => aesKey ?? throw new InvalidCredentialException("Username not provided");

    public async Task<ProblemDetails?> SetKeyFromUserName(string? username)
    {
        if (username == null)
        {
            aesKey = new byte[16];
            return null;
        }

        var result = await webClient.GetKeyForUserName(username).ConfigureAwait(false);

        if (result.Payload is not { } key)
        {
            // The old key is left in place on purpose: nothing has been read or written with the new server yet,
            // and a half-applied key would decrypt the cache into noise.
            return result;
        }

        aesKey = key;
        return null;
    }
}
