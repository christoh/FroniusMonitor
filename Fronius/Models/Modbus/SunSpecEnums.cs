namespace De.Hochstaetter.Fronius.Models.Modbus;

public enum SunSpecMeterType : byte
{
    SinglePhase = 1,
    SplitPhase = 2,
    ThreePhases = 3,
}

public enum SunSpecProtocol : byte
{
    IntAndScaleFactor = 0,
    Float = 1,
}

public enum SunSpecInverterState : ushort
{
    Off = 1,
    Sleeping = 2,
    Starting = 3,
    Mppt = 4,
    Throttled = 5,
    ShuttingDown = 6,
    Fault = 7,
    Standby = 8,

    // Fronius specific
    NoBusInit = 9, // No SolarNet communication
    NoCommInv = 10, // No communication with inverter
    SnOverCurrent = 11, // Over-current on SolarNet plug
    BootLoad = 12, // Update in progress
    Afci = 13, // Arc detected
    Null = 0xffff,
}

public enum SunSpecMpptState : ushort
{
    Off = 1,
    Sleeping = 2,
    Starting = 3,
    Mppt = 4,
    Throttled = 5,
    ShuttingDown = 6,
    Fault = 7,
    Standby = 8,
    Test = 9,
    Reserved10 = 10,
    Null = 0xffff,
}

[Flags]
public enum SunSpecInverterEvents1 : uint
{
    None = 0,
    GroundFault = 1 << 0,
    DcOverVoltage = 1 << 1,
    AcDisconnect = 1 << 2,
    DcDisconnect = 1 << 3,
    GridDisconnect = 1 << 4,
    CabinetOpen = 1 << 5,
    ManualShutdown = 1 << 6,
    OverTemperature = 1 << 7,
    OverFrequency = 1 << 8,
    UnderFrequency = 1 << 9,
    AcOverVoltage = 1 << 10,
    AcUnderVoltage = 1 << 11,
    BlownStringFuse = 1 << 12,
    UnderTemperature = 1 << 13,
    MemoryLoss = 1 << 14,
    HardwareTestFailure = 1 << 15,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecMpptEvent : uint
{
    None = 0,
    GroundFault = 1 << 0,
    InputOverVoltage = 1 << 1,
    Reserved2 = 1 << 2,
    DcDisconnected = 1 << 3,
    Reserved4 = 1 << 4,
    CabinetOpen = 1 << 5,
    ManualShutDown = 1 << 6,
    OverTemperature = 1 << 7,
    Reserved8 = 1 << 8,
    Reserved9 = 1 << 9,
    Reserved10 = 1 << 10,
    Reserved11 = 1 << 11,
    BlownStringFuse = 1 << 12,
    UnderTemperature = 1 << 13,
    MemoryLoss = 1 << 14,
    ArcDetected = 1 << 15,
    Reserved16 = 1 << 16,
    Reserved17 = 1 << 17,
    Reserved18 = 1 << 18,
    Reserved19 = 1 << 19,
    TestFailed = 1 << 20,
    InputUnderVoltage = 1 << 21,
    InputOverCurrent = 1 << 22,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecMeterEvents : uint
{
    None = 0,
    PowerFailure = 1 << 2,
    UnderVoltage = 1 << 3,
    LowPowerFactor = 1 << 4,
    OverCurrent = 1 << 5,
    OverVoltage = 1 << 6,
    MissingSensor = 1 << 7,
    Reserved1 = 1 << 8,
    Reserved2 = 1 << 9,
    Reserved3 = 1 << 10,
    Reserved4 = 1 << 11,
    Reserved5 = 1 << 12,
    Reserved6 = 1 << 13,
    Reserved7 = 1 << 14,
    Reserved8 = 1 << 15,
    Oem1 = 1 << 16,
    Oem2 = 1 << 17,
    Oem3 = 1 << 18,
    Oem4 = 1 << 19,
    Oem5 = 1 << 20,
    Oem6 = 1 << 21,
    Oem7 = 1 << 22,
    Oem8 = 1 << 23,
    Oem9 = 1 << 24,
    Oem10 = 1 << 25,
    Oem11 = 1 << 26,
    Oem12 = 1 << 27,
    Oem13 = 1 << 28,
    Oem14 = 1 << 29,
    Oem15 = 1 << 30,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecConnectionStatus : ushort
{
    None = 0,
    Connected = 1 << 0,
    Available = 1 << 1,
    Operating = 1 << 2,
    Test = 1 << 3,
    Null = 0xffff,
}

[Flags]
public enum SunSpecLimitsReached : uint
{
    None = 0,
    MaxActivePower = 1 << 0,
    MaxApparentPower = 1 << 1,
    MaxReactivePowerAvailable = 1 << 2,
    MaxReactivePowerQ1 = 1 << 3,
    MaxReactivePowerQ2 = 1 << 4,
    MaxReactivePowerQ3 = 1 << 5,
    MaxReactivePowerQ4 = 1 << 6,
    MinimumPowerFactorQ1 = 1 << 7,
    MinimumPowerFactorQ2 = 1 << 8,
    MinimumPowerFactorQ3 = 1 << 9,
    MinimumPowerFactorQ4 = 1 << 10,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecLimitControlsActive : uint
{
    None = 0,
    FixedActivePower = 1 << 0,
    FixedReactivePower = 1 << 1,
    FixedPowerFactor = 1 << 2,
    VoltageReactivePower = 1 << 3,
    FrequencyActivePowerHardLimit = 1 << 4,
    FrequencyActivePowerCurve = 1 << 5,
    DynamicReactiveCurrent = 1 << 6,
    Lvrt = 1 << 7,
    Hvrt = 1 << 8,
    ActivePowerPowerFactor = 1 << 9,
    VoltageActivePower = 1 << 10,
    Scheduled = 1 << 12,
    Lfrt = 1 << 13,
    Hfrt = 1 << 14,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecVoltageRideThroughModesAvailable : ushort
{
    None = 0,
    Lvrt = 1 << 0,
    Hvrt = 1 << 1,
    Lfrt = 1 << 2,
    Hfrt = 1 << 3,
    Null = 0xffff,
}

[Flags]
public enum SunSpecInverterEvents2 : uint
{
    None = 0,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecInverterVendorEvents1 : uint
{
    None = 0,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecInverterVendorEvents2 : uint
{
    None = 0,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecInverterVendorEvents3 : uint
{
    None = 0,
    Null = 0xffffffff,
}

[Flags]
public enum SunSpecInverterVendorEvents4 : uint
{
    None = 0,
    Null = 0xffffffff,
}

public enum SunSpecDerType : ushort
{
    None = 0,
    Inverter = 4,
    Storage = 82,
    Null = 0xffff,
}

public enum SunSpecOnOff : ushort
{
    Disabled = 0,
    Enabled = 1,
    Null = 0xffff,
}

public enum SunSpecConnectionState : ushort
{
    Disconnected = 0,
    Connected = 1,
    Null = 0xffff,
}

[Flags]
public enum SunSpecChargingLimits : ushort
{
    None = 0,
    Charging = 1 << 0,
    Discharging = 1 << 1,
    Null = 0xffff,
}

public enum SunSpecStorageState : ushort
{
    None = 0,
    Off = 1,
    Empty = 2,
    Discharging = 3,
    Charging = 4,
    Full = 5,
    Holding = 6,
    Testing = 7,
    Null = 0xffff,
}

public enum SunSpecChargingSource : ushort
{
    None = 0,
    Pv = 0,
    Grid = 1,
    Null = 0xffff,
}

public enum SunSpecReactivePowerLimitMode : ushort
{
    None = 0,
    RelativeToMaxActivePower = 1,
    RelativeToMaxReactivePower = 2,
    RelativeToAvailableReactivePower = 3,
    Null = 0xffff,

}

public enum SunSpecReactivePowerOnChargeDischargeChange : ushort
{
    Switch = 1,
    Maintain = 2,
    Null = 0xffff,
}

public enum SunSpecTotalApparentPowerCalculationMethod : ushort
{
    Vector = 1,
    Arithmetic = 2,
    Null = 0xffff,
}
