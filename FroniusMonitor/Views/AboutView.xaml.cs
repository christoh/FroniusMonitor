namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class AboutView
{
    public AboutView()
    {
        InitializeComponent();
    }

    public Version DotNetVersion => Environment.Version;
}
