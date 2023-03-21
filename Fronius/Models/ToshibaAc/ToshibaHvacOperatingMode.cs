namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public enum ToshibaHvacOperatingMode : byte
    {
        Auto = 0x41,
        Cooling = 0x42,
        Drying = 0x44,
        Heating = 0x43,
        FanOnly = 0x45,
    }
}
