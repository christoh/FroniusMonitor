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
