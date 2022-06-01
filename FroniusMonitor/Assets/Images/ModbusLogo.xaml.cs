namespace De.Hochstaetter.FroniusMonitor.Assets.Images;

public partial class ModbusLogo
{
    public ModbusLogo()
    {
        InitializeComponent();
    }
}

public class ModbusLogoExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new ModbusLogo();
}
