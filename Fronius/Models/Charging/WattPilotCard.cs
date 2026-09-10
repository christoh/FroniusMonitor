using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

/// <summary>
///     One RFID card. The charger sends it two ways: as an entry of the <c>cards</c> array, under the long names,
///     and as keys of its own - <c>c0n</c>, <c>c0e</c>, <c>c0i</c> for card 0 - under the one-letter ones, which
///     is how a newer firmware reports a single card changing. See <see cref="WattPilot.Cards" />.
/// </summary>
public partial class WattPilotCard : BindableBase, IHaveDisplayName
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [WattPilot("name")]
    [WattPilot("n")]
    [JsonProperty("name")]
    public partial string? Name { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [WattPilot("energy")]
    [WattPilot("e")]
    [JsonProperty("energy")]
    public partial double? Energy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [WattPilot("cardId")]
    [WattPilot("i")]
    [JsonProperty("cardId")]
    public partial bool? HaveCardId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayName => $"{Name ?? Resources.Unknown}: {Energy ?? 0} / {(HaveCardId.HasValue ? HaveCardId.Value : Resources.Unknown)}";

    public override string ToString() => DisplayName;
}