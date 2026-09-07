using System.ComponentModel.DataAnnotations;

namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// The base of the reusable validation rules of this solution. A rule is a <see cref="ValidationAttribute"/>, so it
/// is declared once on the property that is edited and every view that binds to that property gets the same rule -
/// the WPF app through <c>ValidationBinding</c>, the Avalonia client through <see cref="INotifyDataErrorInfo"/>, and
/// ASP.NET Core model binding on the server, which turns a refused request body into a <c>ProblemDetails</c>.
/// </summary>
/// <remarks>
/// <para>
/// The property has to be on a type that validates: <see cref="BindableBase"/> derives from
/// <c>ObservableValidator</c>, and the property needs <c>[NotifyDataErrorInfo]</c> next to
/// <c>[ObservableProperty]</c> so that the generated setter validates what it is given.
/// </para>
/// <para>
/// Every rule reads the value from its text rather than casting it, because a field the user types into binds to a
/// <c>string</c> - see the rule about text input in <c>.claude/rules/ViewModelsForInteractionLogic.md</c>. So the
/// rule, and not a converter, is what decides whether what was typed is a number at all.
/// </para>
/// <para>
/// Messages are put together while the rule runs, never in the constructor, so that they follow the current
/// language. Either the rule composes its own from <see cref="PropertyDisplayName"/>, or
/// <see cref="MessageResourceKey"/> names a resource that already says the whole thing.
/// </para>
/// </remarks>
public abstract class ValidationRuleAttribute : ValidationAttribute
{
    /// <summary>
    /// The name of a resource in <see cref="Resources"/> that is the complete message. Use it where a field
    /// already has a sentence of its own - <c>MeterAddressError</c>, for instance - rather than letting the rule
    /// compose one.
    /// </summary>
    public string? MessageResourceKey { get; set; }

    /// <summary>What to call the field in a composed message. Overrides <see cref="PropertyDisplayNameResourceKey"/>.</summary>
    public string? PropertyDisplayName { get; set; }

    /// <summary>The name of a resource in <see cref="Resources"/> that is what to call the field.</summary>
    public string? PropertyDisplayNameResourceKey { get; set; }

    /// <summary>
    /// Whether a field that has not been filled in passes. It does by default - not having typed anything is not
    /// the same as having typed something wrong, and a field that has to hold a value says so with this.
    /// </summary>
    public bool AllowEmpty { get; set; } = true;

    protected string DisplayName => PropertyDisplayName ?? Localize(PropertyDisplayNameResourceKey) ?? Resources.DefaultPropertyDisplayName;

    /// <summary>
    /// What is wrong with <paramref name="value"/>, in the words of this rule. Only asked for when a value is
    /// refused, and given the value so that a rule can quote it without keeping any state of its own - an
    /// attribute instance is shared by everything that reads it.
    /// </summary>
    protected abstract string Complain(object? value);

    /// <summary>Whether the value - never null and never blank - is one this rule accepts.</summary>
    protected abstract bool IsAcceptable(object? value);

    protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var isEmpty = value is null || (value is string text && string.IsNullOrWhiteSpace(text));

        return isEmpty
            ? AllowEmpty ? ValidationResult.Success : Refuse(value)
            : IsAcceptable(value) ? ValidationResult.Success : Refuse(value);
    }

    /// <summary>
    /// Refuse the value. <see cref="Complain"/> is only asked when <see cref="MessageResourceKey"/> does not name
    /// a message, so a rule can compose one without checking first.
    /// </summary>
    private ValidationResult Refuse(object? value) => new(Localize(MessageResourceKey) ?? Complain(value));

    protected static string? Localize(string? resourceKey) => resourceKey is null ? null : Resources.ResourceManager.GetString(resourceKey, Resources.Culture);
}
