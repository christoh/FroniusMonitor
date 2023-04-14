namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public enum ToshibaHvacMeritFeaturesA : byte // Nibble (half byte): Can only be from 0 to 15
    {
        None = 0,
        HighPower = 1,
        Silent1 = 2,
        Eco = 3,
        Heating8C = 4,
        SleepCare = 5,
        Floor = 6,
        Comfort = 7,
        Silent2 = 10,
    }
}
