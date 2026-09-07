namespace De.Hochstaetter.Fronius.Validators;

/// <summary>
/// Text that has to match a pattern. The counterpart of <c>RegExRule</c> of the WPF app.
/// </summary>
/// <remarks>
/// A pattern says nothing a user can read, so this rule has no message it could compose: give it
/// <see cref="ValidationRuleAttribute.MessageResourceKey"/> or <see cref="Message"/>. Leaving both out is a
/// programming mistake and says so in English, because it is not something an end user can act on.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class RegexRuleAttribute(string pattern) : ValidationRuleAttribute
{
    public string Pattern => pattern;

    /// <summary>The message, where it is not worth a resource of its own. Prefer <see cref="ValidationRuleAttribute.MessageResourceKey"/>.</summary>
    public string? Message { get; set; }

    public RegexOptions Options { get; set; } = RegexOptions.None;

    protected override string Complain(object? value) => Message ?? $"No message was given for the pattern {pattern} of {DisplayName}";

    protected override bool IsAcceptable(object? value) => Regex.IsMatch(value?.ToString() ?? string.Empty, pattern, Options);
}
