namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IWebClientService
    {
        WebConnection? InverterConnection { get; set; }
        WebConnection? FritzBoxConnection { get; set; }
        Task<Gen24System> GetFroniusData(Gen24Components components);
        Task<SystemDevices> GetDevices();
        Task<InverterDevices> GetInverters();
        Task<StorageDevices> GetStorageDevices();
        Task<SmartMeterDevices> GetMeterDevices();
        Task<PowerFlow> GetPowerFlow();
        Task FritzBoxLogin();
        Task<FritzBoxDeviceList> GetFritzBoxDevices();
        Task TurnOnFritzBoxDevice(string ain);
        Task TurnOffFritzBoxDevice(string ain);
        Task SetFritzBoxLevel(string ain, double level);
        Task SetFritzBoxColorTemperature(string ain, double temperatureKelvin);
        Task SetFritzBoxColor(string ain, double hueDegrees, double saturation);
        Task<IOrderedEnumerable<Gen24Event>> GetFroniusEvents();
        Task<T> ReadGen24Entity<T>(string request) where T : new();
        Task<(string JsonString,HttpStatusCode StatusCode)> GetFroniusStringResponse(string request, JToken? token = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null);
        Task<(JToken Token, HttpStatusCode StatusCode)> GetFroniusJsonResponse(string request, JToken? token = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null);
        Task<string> GetConfigString(string category, string key);
        Task<string> GetEventDescription(string code);

    }
}
