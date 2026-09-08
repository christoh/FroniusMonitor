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

    /// <summary>
    /// The string of the inverter followed by something of our own - a colon, usually. The WPF views put the two
    /// in a <c>TextBlock</c> as two <c>Run</c>s; one caption is less to read here.
    /// </summary>
    public ConfigExtension(string key, string suffix)
    {
        text = gen24Loc.GetLocalizedString(Gen24LocalizationSection.Config, key) + suffix;
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

    /// <inheritdoc cref="ConfigExtension(string, string)"/>
    public UiExtension(string key, string suffix)
    {
        text = gen24Loc.GetLocalizedString(Gen24LocalizationSection.Ui, key) + suffix;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => text;
}
