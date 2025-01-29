using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotCard : BindableBase, IHaveDisplayName
{
    [WattPilot("name")]
    [JsonProperty("name")]
    public string? Name
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("energy")]
    [JsonProperty("energy")]
    public double? Energy
    {
        get;
        set => Set(ref field, value);
    }

    [WattPilot("cardId")]
    [JsonProperty("cardId")]
    public bool? HaveCardId
    {
        get;
        set => Set(ref field, value);
    }

    public string DisplayName => $"{Name ?? Resources.Unknown}: {Energy ?? 0} / {(HaveCardId.HasValue ? HaveCardId.Value : Resources.Unknown)}";

    public override string ToString() => DisplayName;
}