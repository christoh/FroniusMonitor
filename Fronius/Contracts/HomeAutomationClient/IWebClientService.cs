namespace De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;

public interface IWebClientService : IDisposable
{
    public Task<byte[]> GetKeyForUserName(string userName);

    public void Initialize(string baseUri);

    public Task<HttpStatusCode> Login(string userName, string password);
}
