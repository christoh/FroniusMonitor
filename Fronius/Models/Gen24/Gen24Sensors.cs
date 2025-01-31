namespace De.Hochstaetter.Fronius.Models.Gen24;

public class Gen24Sensors : BindableBase
{
    public Gen24Inverter? Inverter
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Storage? Storage
    {
        get;
        set => Set(ref field, value);
    }


    public Gen24Restrictions? Restrictions
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24DataManager? DataManager
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24PowerFlow? PowerFlow
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Cache? Cache
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Status? InverterStatus
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Status? MeterStatus
    {
        get;
        set => Set(ref field, value);
    }

    public ObservableCollection<Gen24PowerMeter3P> Meters { get; } = [];

    public Gen24PowerMeter3P? PrimaryPowerMeter => Meters.SingleOrDefault(m => m.Usage == MeterUsage.Inverter);
}