namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24Sensors : BindableBase
{
    
    [ObservableProperty]
    public partial Gen24Inverter? Inverter { get; set; }

    [ObservableProperty]
    public partial Gen24Storage? Storage { get; set; }

    [ObservableProperty]
    [JsonIgnore]
    public partial Gen24PowerFlow? PowerFlow { get; set; }

    [ObservableProperty]
    public partial Gen24Status? InverterStatus { get; set; }

    [ObservableProperty]
    public partial Gen24Status? MeterStatus { get; set; }
    
    [ObservableProperty]
    public partial Gen24StandByStatus? StandByStatus { get; set; }

    public ObservableCollection<Gen24PowerMeter3P> Meters { get; init; } = [];

    public Gen24PowerMeter3P? PrimaryPowerMeter => Meters.SingleOrDefault(m => m.Usage == MeterUsage.Inverter);

    public void GeneratePowerFlow()
    {
        PowerFlow = new()
        {
            SolarPower = Inverter?.SolarPowerSum ?? 0,
            GridPower = PrimaryPowerMeter?.ActivePowerSum ?? 0,
            StoragePower = Inverter?.StoragePower ?? 0,
            LoadPower = -(Inverter?.PowerActiveSum ?? 0) - (PrimaryPowerMeter?.ActivePowerSum ?? 0),
            InverterAcPower = Inverter?.PowerActiveSum ?? 0,
        };
    }
}