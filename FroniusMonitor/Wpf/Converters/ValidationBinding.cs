namespace De.Hochstaetter.FroniusMonitor.Wpf.Converters;

public class ValidationBinding : Binding
{
    public ValidationBinding(string? path) : base(path)
    {
        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        ValidatesOnExceptions = true;
        NotifyOnValidationError = true;
    }

    public ValidationBinding() : this(null) { }

    [DefaultValue(null)]
    public ValidationRule? Rule
    {
        get => ValidationRules.Count == 0 ? null : ValidationRules[0];

        set
        {
            ValidationRules.Clear();
            ValidationRules.Add(value);
        }
    }
}
