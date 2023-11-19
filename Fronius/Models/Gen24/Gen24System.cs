using De.Hochstaetter.Fronius.Services;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public class Gen24System : BindableBase, IHaveDisplayName
{
    private Gen24Config? config;

    public Gen24Config? Config
    {
        get => config;
        set => Set(ref config, value);
    }

    private Gen24Sensors? sensors;

    public Gen24Sensors? Sensors
    {
        get => sensors;
        set => Set(ref sensors, value);
    }

    public required IGen24Service Service { get; init; }

    public string DisplayName => $"{Config?.Versions?.ModelName ?? "GEN24"} - {Config?.InverterSettings?.SystemName ?? "GEN24"} - {Config?.Versions?.SerialNumber ?? "---"}";

    public override string ToString() => DisplayName;
}