using DocumentFormat.OpenXml.Office.PowerPoint.Y2022.M08.Main;

namespace De.Hochstaetter.HomeAutomationClient.Extensions;

internal static class ColorExtensions
{
    public static Color MixWith(this Color color1, Color color2, float percentage = .5f)
    {
        return new Color(
            (byte)Math.Round(color2.A * percentage + color1.A * (1 - percentage), MidpointRounding.ToZero),
            (byte)Math.Round(color2.R * percentage + color1.R * (1 - percentage), MidpointRounding.ToZero),
            (byte)Math.Round(color2.G * percentage + color1.G * (1 - percentage), MidpointRounding.ToZero),
            (byte)Math.Round(color2.B * percentage + color1.B * (1 - percentage), MidpointRounding.ToZero)
        );
    }

    public static Color MultiplyWith(this Color color, float factor)
    {
        return new Color(
            color.A,
            Math.Max((byte)0,Math.Min((byte)255,(byte)Math.Round(color.R * factor, MidpointRounding.ToZero))),
            Math.Max((byte)0,Math.Min((byte)255,(byte)Math.Round(color.G * factor, MidpointRounding.ToZero))),
            Math.Max((byte)0,Math.Min((byte)255,(byte)Math.Round(color.B * factor, MidpointRounding.ToZero)))
        );
    }
}
