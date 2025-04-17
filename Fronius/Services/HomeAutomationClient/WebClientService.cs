using System.Net.Http.Headers;
using De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;

namespace De.Hochstaetter.Fronius.Services.HomeAutomationClient;

public sealed class WebClientService : IWebClientService
{
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(7) };

    public void Initialize(string baseUri)
    {
        var address = new Uri(baseUri);
        httpClient.BaseAddress = address;
    }

    public async Task<byte[]> GetKeyForUserName(string userName)
    {
        var keyString = await httpClient.GetStringAsync($"Identity/requestKey?user={userName}").ConfigureAwait(false);
        return Convert.FromBase64String(keyString);
    }

    public async Task<HttpStatusCode> Login(string userName, string password)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
        using var message = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"Identity/login?user={userName}&password={password}")).ConfigureAwait(false);
        return message.StatusCode;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
