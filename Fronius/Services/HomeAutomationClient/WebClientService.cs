using System.Net.Http.Headers;
using System.Net.Http.Json;
using De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.Fronius.Services.HomeAutomationClient;

public sealed class WebClientService : IWebClientService
{
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(7), };

    public void Initialize(string baseUri, string productName, string version)
    {
        var address = new Uri(baseUri);
        httpClient.BaseAddress = address;
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(productName, version));
    }

    public async Task<byte[]> GetKeyForUserName(string userName, CancellationToken token = default)
    {
        var keyString = await httpClient.GetStringAsync($"Identity/requestKey?user={userName}", token).ConfigureAwait(false);
        return Convert.FromBase64String(keyString);
    }

    public async Task<ProblemDetails?> Login(string userName, string password, CancellationToken token = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
        return await Get($"Identity/login?user={userName}&password={password}", token).ConfigureAwait(false);
    }

    public async Task<ApiResult<IDictionary<string, DeviceInfo>>> ListDevices(CancellationToken token = default)
    {
        return await GetResult<IDictionary<string, DeviceInfo>>("Devices", token).ConfigureAwait(false);
    }

    private async ValueTask<ProblemDetails?> Get(string queryString, CancellationToken token = default)
    {
        HttpResponseMessage? responseMessage = null;

        try
        {
            responseMessage = await httpClient.GetAsync(queryString, token).ConfigureAwait(false);

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                return await GetErrors(responseMessage, token);
            }

            return null;
        }
        catch (Exception ex)
        {
            return new ProblemDetails
            {
                Title = ex.GetType().Name,
                Detail = ex.Message,
                Status = responseMessage?.StatusCode,
            };
        }
        finally
        {
            responseMessage?.Dispose();
        }
    }

    private async ValueTask<ApiResult<T>> GetResult<T>(string queryString, CancellationToken token = default)
    {
        HttpResponseMessage? responseMessage = null;

        try
        {
            responseMessage = await httpClient.GetAsync(queryString, token).ConfigureAwait(false);

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                return ApiResult<T>.FromProblemDetails(await GetErrors(responseMessage, token).ConfigureAwait(false)!);
            }

            return new ApiResult<T> { Payload = await responseMessage.Content.ReadFromJsonAsync<T>(token).ConfigureAwait(false) };
        }
        catch (Exception ex)
        {
            return ApiResult<T>.FromProblemDetails(new ProblemDetails
            {
                Title = ex.GetType().Name,
                Detail = ex.Message,
                Status = responseMessage?.StatusCode,
            });
        }
        finally
        {
            responseMessage?.Dispose();
        }
    }

    private static async ValueTask<ProblemDetails?> GetErrors(HttpResponseMessage message, CancellationToken token)
    {
        return await message.Content.ReadFromJsonAsync<ProblemDetails?>(token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
