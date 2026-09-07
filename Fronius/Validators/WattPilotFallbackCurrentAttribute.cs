namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// The current a WattPilot falls back to when it loses its load balancing: 6 to 32 A, or 0 for none at all.
/// The counterpart of <c>WattPilotFallbackCurrentRule</c> of the WPF app.
/// </summary>
/// <remarks>
/// A range with a hole in it, so <see cref="MinMaxIntAttribute"/> cannot say it. The hole is the point: anything
/// between 1 and 5 A would be accepted by the box and then ignored by the charger.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class WattPilotFallbackCurrentAttribute : ValidationRuleAttribute
{
    protected override string Complain(object? value) => string.Format(CultureInfo.CurrentCulture, Resources.FallbackCurrentError, DisplayName);

    protected override bool IsAcceptable(object? value) =>
        NumericText.TryParseInteger(value?.ToString(), out var current) && current is (>= 6 and <= 32) or 0;
}
