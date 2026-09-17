namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
/// A light whose hue, saturation and value can be set. The defaults say "cannot" - see <see cref="IDimmable"/> for
/// why - except for the two hue properties, which are each other's fallback and so have to be implemented once:
/// a device without a hue answers <see langword="null"/> for <see cref="HueDegrees"/>.
/// </summary>
public interface IHsvColorControl
{
    bool HasHsvColorControl => false;
    bool IsHsvEnabled => false;
    bool IsHsvActive => false;
    double? HueDegrees => HueRadians / Math.PI * 180;
    double? HueRadians => HueDegrees * Math.PI / 180;
    double? Saturation => null;
    double? Value => null;
    Task SetHsv(double hueDegrees, double saturation, double value) => throw new NotSupportedException($"{GetType().Name} has no color control");
}
