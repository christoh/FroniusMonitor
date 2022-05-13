namespace De.Hochstaetter.FroniusMonitor.Models;

public class BatteryColor
{
    public BatteryColor(double soc, Color color)
    {
        Soc = soc;
        Color = color;
    }

    public double Soc { get; }
    public Color Color { get; }
}