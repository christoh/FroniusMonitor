// Aliased rather than imported: ValidationResult here is the WPF one, and importing the data annotations
// namespace would make that name ambiguous throughout the file.
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Validation;

internal class StringResult(Func<string> func) : IHaveDisplayName
{
    public override string ToString() => func();
    public string DisplayName => func();
}

public class MarkupRule(ValidationRuleExtension extension) : ValidationRule
{
    public override ValidationResult Validate(object? value, CultureInfo cultureInfo) => extension.Validate(value);
}

/// <summary>
/// The base of every validation rule a WPF binding can be given, and the bridge to the rules of the solution.
/// </summary>
/// <remarks>
/// <para>
/// **No rule here decides anything.** Each one builds the matching <see cref="ValidationRuleAttribute"/> from
/// <c>Fronius/Validators</c> and asks it, so the WPF app, the Avalonia client and the request validation of the
/// server all refuse the same values with the same words. Put a new rule there, not here; here it only needs the
/// three lines that make it available to XAML.
/// </para>
/// <para>
/// The attribute is built on every call rather than once, because a markup extension is given its properties
/// after it has been constructed and XAML may hand out the same extension to more than one binding.
/// </para>
/// <para>
/// <see cref="AllowEmpty"/> defaults to <see langword="false"/> here, where the attributes default to
/// <see langword="true"/>: every field in this app that carries a rule is a field that has to be filled in, and
/// that was the behaviour before these became wrappers. Set it in the markup where a field may be left empty.
/// </para>
/// </remarks>
public abstract class ValidationRuleExtension : MarkupExtension
{
    private readonly MarkupRule rule;

    protected ValidationRuleExtension()
    {
        rule = new MarkupRule(this);
    }

    /// <summary>The name of a resource in <see cref="Resources"/> that is the complete message.</summary>
    public string? MessageResourceKey { get; set; }

    /// <summary>What to call the field in a composed message.</summary>
    public string? PropertyDisplayName { get; set; }

    /// <summary>The name of a resource in <see cref="Resources"/> that is what to call the field.</summary>
    public string? PropertyDisplayNameResourceKey { get; set; }

    /// <summary>Whether a field that has not been filled in passes. See the remarks on this class.</summary>
    public bool AllowEmpty { get; set; }

    public sealed override object ProvideValue(IServiceProvider serviceProvider) => rule;

    public ValidationResult Validate(object? value)
    {
        var attribute = Configure(CreateRule());
        var context = new ValidationContext(new object());

        return attribute.GetValidationResult(value, context) is null
            ? ValidationResult.ValidResult
            // Lazily, through StringResult: a message that is already on screen when the language changes is asked
            // again and answers in the new one.
            : new ValidationResult(false, new StringResult(() => Configure(CreateRule()).GetValidationResult(value, context)?.ErrorMessage ?? string.Empty));
    }

    /// <summary>The rule of <c>Fronius/Validators</c> that this one stands for.</summary>
    protected abstract ValidationRuleAttribute CreateRule();

    private TAttribute Configure<TAttribute>(TAttribute attribute) where TAttribute : ValidationRuleAttribute
    {
        attribute.MessageResourceKey = MessageResourceKey;
        attribute.PropertyDisplayName = PropertyDisplayName;
        attribute.PropertyDisplayNameResourceKey = PropertyDisplayNameResourceKey;
        attribute.AllowEmpty = AllowEmpty;
        return attribute;
    }
}

internal class RegExRuleExtension : ValidationRuleExtension
{
    public string? Message { get; set; }
    public string Pattern { get; set; } = @"^.*$";

    protected override ValidationRuleAttribute CreateRule() => new RegexRuleAttribute(Pattern) { Message = Message };
}

public class Ipv4OrHostnameExtension : ValidationRuleExtension
{
    protected override ValidationRuleAttribute CreateRule() => new Ipv4Attribute { AllowHostname = true };
}

public class MinMaxFloatRuleExtension : ValidationRuleExtension
{
    public float Minimum { get; set; } = float.MinValue;
    public float Maximum { get; set; } = float.MaxValue;

    protected override ValidationRuleAttribute CreateRule() => new MinMaxDoubleAttribute(Minimum, Maximum);
}

public class MinMaxIntRuleExtension : ValidationRuleExtension
{
    public int Minimum { get; set; }
    public int Maximum { get; set; } = 50000;

    protected override ValidationRuleAttribute CreateRule() => new MinMaxIntAttribute(Minimum, Maximum);
}

public class WattPilotFallbackCurrentRuleExtension : ValidationRuleExtension
{
    protected override ValidationRuleAttribute CreateRule() => new WattPilotFallbackCurrentAttribute();
}

public class ChargingRuleDateExtension : ValidationRuleExtension
{
    protected override ValidationRuleAttribute CreateRule() => new TimeOfDayAttribute();
}
