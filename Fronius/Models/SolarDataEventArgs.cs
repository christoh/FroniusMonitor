namespace De.Hochstaetter.Fronius.Models
{
    public class SolarDataEventArgs:EventArgs
    {
        public SolarDataEventArgs(SolarSystem? solarSystem)
        {
            SolarSystem = solarSystem;
        }

        public SolarSystem? SolarSystem { get; set; }
    }
}
