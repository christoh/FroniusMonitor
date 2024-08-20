namespace De.Hochstaetter.Fronius.Contracts;

public interface IElectricityPrice : ICloneable
{
    public decimal CentsPerKiloWattHour { get; set; }
    public decimal EurosPerKiloWattHour => CentsPerKiloWattHour / 100;
    public DateTime StartTime { get; }
    public DateTime EndTime => StartTime.AddHours(1);
}