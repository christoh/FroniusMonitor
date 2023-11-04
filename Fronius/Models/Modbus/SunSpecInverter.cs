namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecInverter : SunSpecGroupBase
{
    public SunSpecInverter(IEnumerable<SunSpecModelBase> models) : base(models)
    {
        InverterBaseSensors = Models.OfType<ISunSpecInverter>().SingleOrDefault();
        NamePlate = Models.OfType<SunSpecNamePlate>().SingleOrDefault();
        BasicSettings = Models.OfType<SunSpecInverterBaseSettings>().SingleOrDefault();
        ExtendedSensors = Models.OfType<SunSpecInverterExtendedSensors>().SingleOrDefault();
        ExtendedSettings = Models.OfType<SunSpecInverterExtendedSettings>().SingleOrDefault();
        StorageSettings = Models.OfType<SunSpecStorageBaseSettings>().SingleOrDefault();
        Tracker = Models.OfType<SunSpecMultipleMppt>().SingleOrDefault();
    }

    public ISunSpecInverter? InverterBaseSensors { get; }

    public SunSpecNamePlate? NamePlate { get; }

    public SunSpecInverterBaseSettings? BasicSettings { get; }

    public SunSpecInverterExtendedSensors? ExtendedSensors { get; }

    public SunSpecInverterExtendedSettings? ExtendedSettings { get; }

    public SunSpecStorageBaseSettings? StorageSettings { get; }

    public SunSpecMultipleMppt? Tracker { get; }
}