namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// A whole number within an inclusive range. The counterpart of <c>MinMaxIntRule</c> of the WPF app.
/// </summary>
/// <remarks>
/// Text that is not a whole number at all is refused with the same message: a field that wants a number between 1
/// and 247 has said everything there is to say about "abc".
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinMaxIntAttribute(long minimum, long maximum) : ValidationRuleAttribute
{
    public long Minimum => minimum;

    public long Maximum => maximum;

    protected override string Complain(object? value) => string.Format(CultureInfo.CurrentCulture, Resources.MustBeBetween, DisplayName, minimum, maximum);

    protected override bool IsAcceptable(object? value) =>
        NumericText.TryParseInteger(value?.ToString(), out var number) && number >= minimum && number <= maximum;
}

/// <summary>
/// A number within an inclusive range, decimals allowed. The counterpart of <c>MinMaxFloatRule</c> of the WPF app.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinMaxDoubleAttribute(double minimum, double maximum) : ValidationRuleAttribute
{
    public double Minimum => minimum;

    public double Maximum => maximum;

    protected override string Complain(object? value) => string.Format(CultureInfo.CurrentCulture, Resources.MustBeBetween, DisplayName, minimum, maximum);

    protected override bool IsAcceptable(object? value) =>
        NumericText.TryParseNumber(value?.ToString(), out var number) && number >= minimum && number <= maximum;
}
