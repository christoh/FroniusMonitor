namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
/// A consumer whose level can be set. The defaults say "cannot": a device that implements
/// <see cref="IPowerConsumer1P"/> for its power meter alone - a wallbox, a heat pump - is not dimmable and should
/// not have to say so in four members of its own. Whoever can dim overrides all four, as <c>FritzBoxDevice</c> does.
/// </summary>
public interface IDimmable
{
    bool CanDim => false;
    double? Level => null;
    Task SetLevel(double level) => throw new NotSupportedException($"{GetType().Name} cannot be dimmed");
    bool IsDimmingEnabled => false;
}
