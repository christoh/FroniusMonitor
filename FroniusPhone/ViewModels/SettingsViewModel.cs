namespace FroniusPhone.ViewModels;

public class SettingsViewModel(Settings settings) : ViewModelBase
{
    private static readonly IEnumerable<string> gen24UserNames = new[] {"customer", "technician", "support"};

    public IEnumerable<string> Gen24UserNames => gen24UserNames;

    private static readonly IReadOnlyList<byte> froniusUpdateRates = new byte[] {1, 2, 3, 4, 5, 10, 20, 30, 60};

    public IReadOnlyList<byte> FroniusUpdateRates => froniusUpdateRates;

    public Settings Settings
    {
        get => settings;
        set => Set(ref settings, value);
    }
}