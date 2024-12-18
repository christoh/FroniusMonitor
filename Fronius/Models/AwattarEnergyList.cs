﻿namespace De.Hochstaetter.Fronius.Models
{
    public class AwattarEnergyList : BindableBase
    {
        private List<AwattarEnergy> energies = [];

        [JsonPropertyName("data")]
        public List<AwattarEnergy> Energies
        {
            get => energies;
            set => Set(ref energies, value);
        }

        private IEnumerable<AwattarPriceComponent>? prices;
        [JsonIgnore]
        public IEnumerable<AwattarPriceComponent>? Prices
        {
            get => prices;
            set => Set(ref prices, value);
        }
    }
}
