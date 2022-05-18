namespace De.Hochstaetter.FroniusMonitor.Wpf.Localization
{
    public class Loc : MarkupExtension
    {
        private readonly string category;
        private readonly string key;
        private readonly IWebClientService webClientService = IoC.TryGet<IWebClientService>();

        public Loc(string category, string key)
        {
            this.category = category;
            this.key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => webClientService?.GetConfigString(category, key).Result ?? "Text from Inverter";
    }
}
