namespace De.Hochstaetter.Fronius.Models
{
    public class WebConnection : BindableBase
    {
        public string BaseUrl { get; init; } = string.Empty;
        public string UserName { get; init; } = "FroniusMonitor";
        public string Password { get; init; } = "Password";
    }
}
