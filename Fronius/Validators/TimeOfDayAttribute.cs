namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// A time of day from <c>00:00</c> to <c>24:00</c>, as the charging rules of an inverter and the trip times of a
/// WattPilot are written. The counterpart of <c>ChargingRuleDate</c> of the WPF app.
/// </summary>
/// <remarks>
/// <c>24:00</c> is the end of the day and is accepted; <c>24:30</c> is not. The pattern is the one the model
/// itself reads these times with, <see cref="Gen24ChargingRule.TimeRegex"/>, so a value this rule passes is one
/// the model can parse.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TimeOfDayAttribute : ValidationRuleAttribute
{
    protected override string Complain(object? value) => string.Format(CultureInfo.CurrentCulture, Resources.InvalidChargingRuleTime, value);

    protected override bool IsAcceptable(object? value)
    {
        var match = Gen24ChargingRule.TimeRegex().Match(value?.ToString() ?? string.Empty);

        if (!match.Success)
        {
            return false;
        }

        var hours = byte.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minutes = byte.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        return hours <= 24 && minutes <= 59 && (hours != 24 || minutes == 0);
    }
}
