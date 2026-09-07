namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// Reading a number out of what a user typed, and putting one back into a box. A field the user types into binds
/// to a <c>string</c> - see the rule about text input in <c>.claude/rules/ViewModelsForInteractionLogic.md</c> -
/// so a view model has to do the two conversions itself.
/// </summary>
/// <remarks>
/// It lives beside <see cref="MinMaxIntAttribute"/> and <see cref="MinMaxDoubleAttribute"/> because those rules
/// read the value the same way. A view model that parsed differently from the rule that let the text through
/// would throw at the moment of saving, and only for some inputs.
/// </remarks>
public static class NumericText
{
    private const NumberStyles IntegerStyles =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands;

    public static bool TryParseInteger(string? text, out long value) => long.TryParse(text, IntegerStyles, CultureInfo.CurrentCulture, out value);

    public static bool TryParseNumber(string? text, out double value) => double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value);

    /// <summary>
    /// What a rule has already accepted, as the number it stands for, or null where the field is empty. Only for
    /// text a rule has passed: the exception means something validated one way and saved another.
    /// </summary>
    public static long? ToInteger(string? text) => string.IsNullOrWhiteSpace(text)
        ? null
        : TryParseInteger(text, out var value)
            ? value
            : throw new InvalidOperationException($"'{text}' is not a whole number. A rule should have refused it before it got here.");

    public static double? ToNumber(string? text) => string.IsNullOrWhiteSpace(text)
        ? null
        : TryParseNumber(text, out var value)
            ? value
            : throw new InvalidOperationException($"'{text}' is not a number. A rule should have refused it before it got here.");

    /// <summary>A value on its way into a box, or null where there is none to show.</summary>
    public static string? Of(long? value, string? format = null) => value?.ToString(format, CultureInfo.CurrentCulture);

    public static string? Of(double? value, string? format = null) => value?.ToString(format, CultureInfo.CurrentCulture);
}
