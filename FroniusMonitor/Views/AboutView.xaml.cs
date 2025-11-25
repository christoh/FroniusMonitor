namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class AboutView
{
    public AboutView()
    {
        InitializeComponent();
    }

#if DEBUG
private const string Configuration = "debug";
#else
private const string Configuration = "release";
#endif

    public Version DotNetVersion => Environment.Version;
    public Version WindowsVersion => Environment.OSVersion.Version;
    public string BuildInfo => $"{Configuration}:{App.BranchName}:{App.GitCommitId}";
}
