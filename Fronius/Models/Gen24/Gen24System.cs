using DocumentFormat.OpenXml;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public class Gen24System : BindableBase, IHaveDisplayName, IHaveUniqueId
{
    public Gen24Config? Config
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Sensors? Sensors
    {
        get;
        set => Set(ref field, value);
    }

    [JsonIgnore] public IGen24Service Service { get; init; } = null!;

    [JsonIgnore] public string DisplayName => $"{Config?.Versions?.ModelName ?? "GEN24"} - {Config?.InverterSettings?.SystemName ?? "GEN24"} - {Config?.Versions?.SerialNumber ?? "---"}";

    public override string ToString() => DisplayName;
    public bool IsPresent => true;
    public string? Manufacturer => "Fronius";
    public string? Model => Config?.Versions?.ModelName;
    public string? SerialNumber => Sensors?.DataManager?.SerialNumber ?? Config?.Versions?.SerialNumber;
}
