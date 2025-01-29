namespace De.Hochstaetter.Fronius.Models
{
    public class AwattarEnergyList : BindableBase
    {
        [JsonPropertyName("data")]
        public List<AwattarEnergy> Energies
        {
            get;
            set => Set(ref field, value);
        } = [];

        [JsonIgnore]
        public IEnumerable<AwattarPriceComponent>? Prices
        {
            get;
            set => Set(ref field, value);
        }
    }
}
