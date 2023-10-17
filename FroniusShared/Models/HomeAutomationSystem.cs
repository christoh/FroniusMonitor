namespace De.Hochstaetter.FroniusShared.Models;

public class HomeAutomationSystem : BindableBase
{
    private Gen24Sensors? gen24Sensors;
    private static readonly IList<SmartMeterCalibrationHistoryItem> history = IoC.TryGet<IDataCollectionService>()?.SmartMeterHistory!;

    public Gen24Sensors? Gen24Sensors
    {
        get => gen24Sensors;
        set => Set(ref gen24Sensors, value);
    }

    private Gen24Sensors? gen24Sensors2;

    public Gen24Sensors? Gen24Sensors2
    {
        get => gen24Sensors2;
        set => Set(ref gen24Sensors2, value);
    }

    private Gen24Config? gen24Config;

    public Gen24Config? Gen24Config
    {
        get => gen24Config;
        set => Set(ref gen24Config, value);
    }

    private Gen24Config? gen24Config2;

    public Gen24Config? Gen24Config2
    {
        get => gen24Config2;
        set => Set(ref gen24Config2, value);
    }

    private FritzBoxDeviceList? fritzBox;

    public FritzBoxDeviceList? FritzBox
    {
        get => fritzBox;
        set => Set(ref fritzBox, value);
    }

    private Gen24PowerFlow? sitePowerFlow;

    public Gen24PowerFlow? SitePowerFlow
    {
        get => sitePowerFlow;
        set => Set(ref sitePowerFlow, value, () =>
        {
            NotifyOfPropertyChange(nameof(GridPowerCorrected));
            NotifyOfPropertyChange(nameof(LoadPowerCorrected));
        });
    }

    private WattPilot? wattPilot;

    public WattPilot? WattPilot
    {
        get => wattPilot;
        set => Set(ref wattPilot, value);
    }

    private static int oldSmartMeterHistoryCountProduced;
    private static int oldSmartMeterHistoryCountConsumed;

    private static double consumedFactor = 1;

    public static double ConsumedFactor
    {
        get
        {
            if (oldSmartMeterHistoryCountConsumed != history.Count)
            {
                var consumed = (IReadOnlyList<SmartMeterCalibrationHistoryItem>)history.Where(item => double.IsFinite(item.ConsumedOffset)).ToList();
                consumedFactor = CalculateSmartMeterFactor(consumed, false);
                oldSmartMeterHistoryCountConsumed = history.Count;
            }

            return consumedFactor;
        }
    }

    private static double producedFactor = 1;

    public double ProducedFactor
    {
        get
        {
            if (oldSmartMeterHistoryCountProduced != history.Count)
            {
                var produced = (IReadOnlyList<SmartMeterCalibrationHistoryItem>)history.Where(item => double.IsFinite(item.ProducedOffset)).ToList();
                producedFactor = CalculateSmartMeterFactor(produced, true);
                oldSmartMeterHistoryCountProduced = history.Count;
            }

            return producedFactor;
        }
    }

    public double? LoadPowerCorrected => SitePowerFlow?.LoadPower + SitePowerFlow?.GridPower - GridPowerCorrected;

    public double? GridPowerCorrected => SitePowerFlow?.GridPower * (SitePowerFlow?.GridPower < 0 ? ProducedFactor : ConsumedFactor);

    private static double CalculateSmartMeterFactor(IReadOnlyList<SmartMeterCalibrationHistoryItem> list, bool isProduced)
    {
        if (list.Count < 2)
        {
            return 1.0;
        }

        var first = list[0];
        var last = list[^1];
        var rawEnergy = (isProduced ? last.EnergyRealProduced : last.EnergyRealConsumed) - (isProduced ? first.EnergyRealProduced : first.EnergyRealConsumed);
        var offsetEnergy = (isProduced ? last.ProducedOffset : last.ConsumedOffset) - (isProduced ? first.ProducedOffset : first.ConsumedOffset);
        return (rawEnergy + offsetEnergy) / rawEnergy;
    }
}

