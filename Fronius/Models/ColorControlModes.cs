namespace De.Hochstaetter.Fronius.Models;

[Flags]
public enum ColorControlModes
{
    None = 0,
    Hsv = 1 << 0,
    Rgb = 1 << 1,
    Temperature = 1 << 2,
}