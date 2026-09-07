using System.Collections;

namespace De.Hochstaetter.HomeAutomationClient.Converters;

/// <summary>
/// The validation errors of a control as one piece of text, or null where there are none. Bind
/// <c>ToolTip.Tip</c> to <c>$self.(DataValidationErrors.Errors)</c> through this and a refused field explains
/// itself when the pointer rests on it - see <c>Styles/Validation.axaml</c>, which does it for every text box.
/// </summary>
/// <remarks>
/// A converter rather than an indexer in the binding: <c>(DataValidationErrors.Errors)[0]</c> does not compile,
/// because compiled bindings cannot index an <see cref="IEnumerable{T}"/>. Taking the whole list is better anyway,
/// since a field can be refused for more than one reason at once.
/// </remarks>
public class ValidationErrors : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var messages = (value is IEnumerable errors and not string ? errors.Cast<object?>() : [value])
            .Select(Describe)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToList();

        return messages.Count == 0 ? null : string.Join(Environment.NewLine, messages);
    }

    /// <summary>
    /// A rule of ours arrives as a <see cref="ValidationResult"/>. An exception would mean a binding refused the
    /// value itself, which is what binding a text box to a string is there to prevent - see the rule about text
    /// input in <c>.claude/rules/ViewModelsForInteractionLogic.md</c> - so its own message is shown as it is,
    /// because it is a defect and not something the user can act on.
    /// </summary>
    private static string? Describe(object? error) => error switch
    {
        null => null,
        ValidationResult result => result.ErrorMessage,
        Exception exception => exception.Message,
        _ => error.ToString(),
    };
}
