namespace De.Hochstaetter.Fronius.Models
{
    public partial class ElectricityPrice : BindableBase, IElectricityPrice
    {
        [ObservableProperty]
        public partial decimal CentsPerKiloWattHour { get; set; }
        
        public DateTime StartTime { get; init; }

        public DateTime EndTime { get; init; }

        public object Clone() => MemberwiseClone();
    }
}
