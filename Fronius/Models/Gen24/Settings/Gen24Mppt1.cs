﻿namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Mppt1 : Gen24MpptBase
{
    [FroniusProprietaryImport("PV_VOLTAGE_FIX_01_F32", FroniusDataType.Root)]
    public override double? DcFixedVoltage
    {
        get => base.DcFixedVoltage;
        set => base.DcFixedVoltage = value;
    }

    [FroniusProprietaryImport("PV_MODE_DYNAMICPEAK_01_U16", FroniusDataType.Root)]
    public override MpptOnOff? DynamicPeakManager
    {
        get => base.DynamicPeakManager;
        set => base.DynamicPeakManager = value;
    }

    [FroniusProprietaryImport("PV_MODE_MPP_01_U16", FroniusDataType.Root)]
    public override MpptPowerMode? PowerMode
    {
        get => base.PowerMode;
        set => base.PowerMode = value;
    }

    [FroniusProprietaryImport("PV_POWERACTIVE_CONNECTED_PEAK_MAX_01_U32", FroniusDataType.Root)]
    public override uint? WattPeak
    {
        get => base.WattPeak;
        set => base.WattPeak = value;
    }
}
