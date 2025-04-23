using System.Net.Http;
using System.Security.Authentication;

namespace De.Hochstaetter.HomeAutomationClient.Crypto;

public class AesKeyProvider(IWebClientService webClient) : IServerBasedAesKeyProvider
{
    private byte[]? aesKey;

    public byte[] GetAesKey() => aesKey ?? throw new InvalidCredentialException("Username not provided");

    public async Task SetKeyFromUserName(string? username)
    {
        if (username == null)
        {
            aesKey = new byte[16];
            return;
        }

        aesKey = await webClient.GetKeyForUserName(username).ConfigureAwait(false);
    }
}
