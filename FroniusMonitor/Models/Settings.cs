namespace De.Hochstaetter.FroniusMonitor.Models;

public class Settings : SettingsShared
{
    private Size? mainWindowSize;
    [DefaultValue(null),XmlElement("WindowSize")]
    public Size? MainWindowSize
    {
        get => mainWindowSize;
        set => Set(ref mainWindowSize, value);
    }

    private double controllerGridRowHeight = 375;
    [DefaultValue(375), XmlElement("ControllerGridRowHeight")]
    public double ControllerGridRowHeight
    {
        get => controllerGridRowHeight;
        set => Set(ref controllerGridRowHeight, value);
    }

    private bool showRibbon;
    [DefaultValue(false), XmlAttribute("ShowRibbon")]
    public bool ShowRibbon
    {
        get => showRibbon;
        set => Set(ref showRibbon, value);
    }

    private string? customSolarPanelLayout;
    [DefaultValue(null)]
    public string? CustomSolarPanelLayout
    {
        get => customSolarPanelLayout;
        set => Set(ref customSolarPanelLayout, value);
    }

    public static Task Save() => Save(App.SettingsFileName);

    public static async Task Save(string fileName) => await Task.Run(() => Save(App.Settings, fileName)).ConfigureAwait(false);

    public static async Task Load(string fileName) => await Task.Run(() =>
    {
        try
        {
            App.SolarSystemQueryTimer = new(_ => { Environment.Exit(0); }, null, 10000, -1);
            App.Settings = Load<Settings>(fileName);
        }
        finally
        {
            App.SolarSystemQueryTimer?.Dispose();
        }
    }).ConfigureAwait(false);

    public static Task Load() => Load(App.SettingsFileName);
}
