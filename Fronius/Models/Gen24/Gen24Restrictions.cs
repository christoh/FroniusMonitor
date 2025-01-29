namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Restrictions : Gen24DeviceBase
{
    [FroniusProprietaryImport("BAT_MODE_POWERRESTRICTION_CHARGE_FROM_AC_U16")]
    public double? MaxStorageChargeFromGridPower
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_PERCENT_POWERRESTRICTION_SOC_MAX_F64", Unit.Percent)]
    public double MaxStateOfCharge
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_PERCENT_POWERRESTRICTION_SOC_MIN_F64", Unit.Percent)]
    public double MinStateOfCharge
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DCLINK_POWERACTIVE_LIMIT_DISCHARGE_F64")]
    public double? StorageLimitDischarge
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DCLINK_POWERACTIVE_MAX_F32")]
    public double? StorageLimitCharge
    {
        get;
        set => Set(ref field, value);
    }
}