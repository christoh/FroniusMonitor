namespace De.Hochstaetter.HomeAutomationClient.Extensions;

public class Loc24Extension : MarkupExtension
{
    private readonly IGen24LocalizationService gen24Loc = IoC.GetRegistered<IGen24LocalizationService>();
    private readonly string text;


    public Loc24Extension(Gen24LocalizationSection section, string key)
    {
        text = gen24Loc.GetLocalizedString(section, key);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => text;
}

public class ChannelExtension : MarkupExtension
{
    private readonly IGen24LocalizationService gen24Loc = IoC.GetRegistered<IGen24LocalizationService>();
    private readonly string text;
    
    public ChannelExtension(string key)
    {
        text = gen24Loc.GetLocalizedString(Gen24LocalizationSection.Channels, key);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => text;
}

public class ConfigExtension : MarkupExtension
{
    private readonly IGen24LocalizationService gen24Loc = IoC.GetRegistered<IGen24LocalizationService>();
    private readonly string text;
    
    public ConfigExtension(string key)
    {
        text = gen24Loc.GetLocalizedString(Gen24LocalizationSection.Config, key);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => text;
}

public class UiExtension : MarkupExtension
{
    private readonly IGen24LocalizationService gen24Loc = IoC.GetRegistered<IGen24LocalizationService>();
    private readonly string text;
    
    public UiExtension(string key)
    {
        text = gen24Loc.GetLocalizedString(Gen24LocalizationSection.Ui, key);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => text;
}
