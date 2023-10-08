namespace De.Hochstaetter.Fronius.Models.Modbus;

public enum SunSpecMeterType : byte
{
    SinglePhase = 1,
    SplitPhase = 2,
    ThreePhases = 3,
}

public enum SunSpecProtocol : byte
{
    IntAndScale = 0,
    Float = 1,
}