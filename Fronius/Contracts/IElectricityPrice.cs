namespace De.Hochstaetter.Fronius.Contracts;

public interface IElectricityPrice
{
    public decimal CentsPerKiloWattHour { get; }
    public decimal EurosPerKiloWattHour => CentsPerKiloWattHour / 100;
    public DateTime StartTime { get; }
    public DateTime EndTime => StartTime.AddHours(1);
}