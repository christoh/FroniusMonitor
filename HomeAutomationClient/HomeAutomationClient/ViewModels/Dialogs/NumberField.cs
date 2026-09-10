using System.Collections;
using System.ComponentModel;
using De.Hochstaetter.Fronius.Validators;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// One number the user may edit two ways, as text in a box and as a position on a slider, kept in step.
/// </summary>
/// <remarks>
/// <para>
/// The box binds to <see cref="Text"/>, a string, for the reason every text input in this app binds to a string
/// (<c>.claude/rules/ViewModelsForInteractionLogic.md</c>): whatever the user types is stored, so it can be
/// validated, it notifies, and Undo reaches the control. The slider binds to <see cref="Slider"/>, a double,
/// because a slider cannot produce a value out of its range. They are two views of one number, and one guard
/// stops them chasing each other: whichever is written first does the writing, and the change it causes in the
/// other is ignored - the pattern of <c>Gen24SelfConsumptionViewModel</c>, here in a class of its own because the
/// Wattpilot dialog has thirty such pairs and could not carry them one by one.
/// </para>
/// <para>
/// The rule is an instance of one of the attributes in <c>Fronius/Validators</c>, given at construction rather
/// than declared on a property, because the limits differ per field and an attribute cannot be parameterized per
/// instance. It is the same rule, asked the same way, so a field here refuses the same values with the same words
/// as a settings model on the server or a box in the WPF app - which is what the wrappers of the WPF app do too.
/// So this type reports through <see cref="INotifyDataErrorInfo"/> itself rather than through
/// <c>ObservableValidator</c>, whose validation only knows attributes on properties.
/// </para>
/// <para>
/// Text a rule refuses moves nothing: a half typed number is not a position, and a slider jumping about while a
/// box is being filled in would be worse than one that waits. A valid number outside the slider's own range - the
/// rule may allow more than the slider offers - pins the slider at its end without touching the text; clamping it
/// here is what keeps the slider from coercing and writing the clamped value back over what the user typed.
/// </para>
/// </remarks>
public sealed partial class NumberField : ObservableObject, INotifyDataErrorInfo
{
    private readonly ValidationRuleAttribute rule;
    private readonly int decimals;
    private string? error;
    private bool isSyncing;

    /// <param name="rule">What the text has to satisfy; its message is what the field shows.</param>
    /// <param name="minimum">The slider's range, which may be narrower than the rule's.</param>
    /// <param name="tickFrequency">The slider's step, and with it the number of decimals the box shows.</param>
    /// <param name="decimals">How many decimals a slider position is written into the box with.</param>
    public NumberField(ValidationRuleAttribute rule, double minimum, double maximum, double tickFrequency = 1, int decimals = 0)
    {
        this.rule = rule;
        this.decimals = decimals;
        Minimum = minimum;
        Maximum = maximum;
        TickFrequency = tickFrequency;
        Slider = minimum;
    }

    /// <summary>The slider's range. Settable, because some limits follow another field - the maximum current a cable takes.</summary>
    [ObservableProperty]
    public partial double Minimum { get; set; }

    [ObservableProperty]
    public partial double Maximum { get; set; }

    public double TickFrequency { get; }

    /// <summary>What the box holds. Anything the user typed, valid or not.</summary>
    [ObservableProperty]
    public partial string? Text { get; set; }

    /// <summary>Where the slider sits. Always within the range, always a number.</summary>
    [ObservableProperty]
    public partial double Slider { get; set; }

    /// <summary>Why the text is refused, or null while it is not.</summary>
    public string? Error => error;

    public bool HasErrors => error is not null;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName) => error is not null && propertyName is null or nameof(Text)
        ? new[] { new System.ComponentModel.DataAnnotations.ValidationResult(error) }
        : Array.Empty<System.ComponentModel.DataAnnotations.ValidationResult>();

    /// <summary>What the box holds as a number, or null while the rule refuses it or the box is empty.</summary>
    public double? Value => error is null && NumericText.TryParseNumber(Text, out var value) ? value : null;

    public long? IntegerValue => Value is { } value ? (long)Math.Round(value, MidpointRounding.AwayFromZero) : null;

    /// <summary>
    /// A value from the device into both halves at once, and validated afterwards: a setter validates only what it
    /// actually stores, so loading the same value twice - at load, and again after Undo - would otherwise leave
    /// whatever error was there standing.
    /// </summary>
    public void Load(double? value)
    {
        Guard(() =>
        {
            Text = value is null ? null : Format(value.Value);
            Slider = value is null ? Minimum : Math.Clamp(value.Value, Minimum, Maximum);
        });

        Validate();
    }

    partial void OnTextChanged(string? value)
    {
        Validate();

        Guard(() =>
        {
            if (Value is { } number)
            {
                Slider = Math.Clamp(number, Minimum, Maximum);
            }
        });
    }

    partial void OnSliderChanged(double value) => Guard(() => Text = Format(value));

    private void Validate()
    {
        var result = rule.GetValidationResult(Text, new System.ComponentModel.DataAnnotations.ValidationContext(this));
        var message = result is null || result == System.ComponentModel.DataAnnotations.ValidationResult.Success ? null : result.ErrorMessage;

        if (message == error)
        {
            return;
        }

        error = message;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Text)));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(Error));
    }

    private string Format(double value) => Math.Round(value, decimals).ToString("F" + decimals, CultureInfo.CurrentCulture);

    private void Guard(Action write)
    {
        if (isSyncing)
        {
            return;
        }

        isSyncing = true;

        try
        {
            write();
        }
        finally
        {
            isSyncing = false;
        }
    }
}
