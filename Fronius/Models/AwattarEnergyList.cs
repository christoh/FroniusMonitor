namespace De.Hochstaetter.Fronius.Models
{
    public partial class AwattarEnergyList : BindableBase
    {
        [ObservableProperty]
        [JsonPropertyName("data")]
        public partial List<AwattarEnergy> Energies { get; set; } = [];

        [ObservableProperty]
        [JsonIgnore]
        public partial IEnumerable<AwattarPriceComponent>? Prices { get; set; }
    }
}
