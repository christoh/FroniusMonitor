namespace De.Hochstaetter.FroniusMonitor.Wpf.Localization;

public class LocConfig : MarkupExtension
{
    private readonly string category;
    private readonly string key;
    private readonly IWebClientService webClientService = IoC.TryGet<IWebClientService>();

    public LocConfig(string category, string key)
    {
        this.category = category;
        this.key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => webClientService?.GetConfigString(category, key).Result ?? "Text from Inverter";
}

public class LocUi : MarkupExtension
{
    private readonly string category;
    private readonly string key;
    private readonly IWebClientService webClientService = IoC.TryGet<IWebClientService>();

    public LocUi(string category, string key)
    {
        this.category = category;
        this.key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => webClientService?.GetUiString(category, key).Result ?? "Text from Inverter";
}