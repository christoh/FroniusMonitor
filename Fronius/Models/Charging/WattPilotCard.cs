using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging
{
    public class WattPilotCard : BindableBase, IHaveDisplayName
    {
        private string? name;

        [WattPilot("name")]
        [JsonProperty("name")]
        public string? Name
        {
            get => name;
            set => Set(ref name, value);
        }

        private double? energy;

        [WattPilot("energy")]
        [JsonProperty("energy")]
        public double? Energy
        {
            get => energy;
            set => Set(ref energy, value);
        }

        private bool? haveCardId;

        [WattPilot("cardId")]
        [JsonProperty("cardId")]
        public bool? HaveCardId
        {
            get => haveCardId;
            set => Set(ref haveCardId, value);
        }

        public string DisplayName => $"{Name ?? Resources.Unknown}: {Energy ?? 0} / {(HaveCardId.HasValue ? HaveCardId.Value : Resources.Unknown)}";

        public override string ToString() => DisplayName;
    }
}
