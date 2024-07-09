namespace De.Hochstaetter.Fronius.Models.ToshibaAc;

public enum ToshibaHvacPowerSetting : byte
{
    Normal = 0x0,
    Eco = 0x3,
    HiPower = 0x1,
    Quiet1 = 0xa,
    Quiet2 = 0x2,
}