using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

public partial class WattPilotCard : BindableBase, IHaveDisplayName
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [WattPilot("name")]
    [JsonProperty("name")]
    public partial string? Name { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [WattPilot("energy")]
    [JsonProperty("energy")]
    public partial double? Energy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [WattPilot("cardId")]
    [JsonProperty("cardId")]
    public partial bool? HaveCardId { get; set; }

    public string DisplayName => $"{Name ?? Resources.Unknown}: {Energy ?? 0} / {(HaveCardId.HasValue ? HaveCardId.Value : Resources.Unknown)}";

    public override string ToString() => DisplayName;
}