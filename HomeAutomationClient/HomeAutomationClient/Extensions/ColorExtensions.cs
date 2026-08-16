using DocumentFormat.OpenXml.Office.PowerPoint.Y2022.M08.Main;

namespace De.Hochstaetter.HomeAutomationClient.Extensions;

internal static class ColorExtensions
{
    extension(Color color1)
    {
        public Color MixWith(Color color2, float percentage = .5f)
        {
            return new Color(
                (byte)Math.Round(color2.A * percentage + color1.A * (1 - percentage), MidpointRounding.ToZero),
                (byte)Math.Round(color2.R * percentage + color1.R * (1 - percentage), MidpointRounding.ToZero),
                (byte)Math.Round(color2.G * percentage + color1.G * (1 - percentage), MidpointRounding.ToZero),
                (byte)Math.Round(color2.B * percentage + color1.B * (1 - percentage), MidpointRounding.ToZero)
            );
        }

        public Color MultiplyWith(float factor)
        {
            return new Color(
                color1.A,
                Math.Max((byte)0,Math.Min((byte)255,(byte)Math.Round(color1.R * factor, MidpointRounding.ToZero))),
                Math.Max((byte)0,Math.Min((byte)255,(byte)Math.Round(color1.G * factor, MidpointRounding.ToZero))),
                Math.Max((byte)0,Math.Min((byte)255,(byte)Math.Round(color1.B * factor, MidpointRounding.ToZero)))
            );
        }
    }
}
