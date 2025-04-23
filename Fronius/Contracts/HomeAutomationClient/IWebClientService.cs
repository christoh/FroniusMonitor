using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.Fronius.Contracts.HomeAutomationClient;

public interface IWebClientService : IDisposable
{
    Task<byte[]> GetKeyForUserName(string userName, CancellationToken token = default);

    void Initialize(string baseUri, string productName, string version);

    Task<ProblemDetails?> Login(string userName, string password, CancellationToken token = default);

    Task<ApiResult<IDictionary<string, DeviceInfo>>> ListDevices(CancellationToken token = default);
    
    Task<ApiResult<IDictionary<string, Gen24System>>> GetGen24Devices(CancellationToken token = default);
}
