namespace De.Hochstaetter.FroniusMonitor.Wpf.Validation;

internal class RegexRule : ValidationRule
{
    private readonly string pattern;
    private readonly string message;

    public RegexRule(string pattern, string message)
    {
        this.pattern = pattern;
        this.message = message;
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is not string text)
        {
            return new ValidationResult(false, $"Must bind to {nameof(String)}");
        }

        if (!Regex.IsMatch(text, pattern))
        {
            return new ValidationResult(false, message);
        }

        return ValidationResult.ValidResult;
    }
}

internal class RegExRuleExtension : MarkupExtension
{
    public string Message { get; set; } = string.Empty;
    public string Pattern { get; set; } = @"^.*$";

    public override object ProvideValue(IServiceProvider serviceProvider) => new RegexRule(Pattern, Message);
}

public class MinMaxFloatRule : ValidationRule
{
    private readonly float minimum;
    private readonly float maximum;
    private readonly string propertyDisplayName;

    public MinMaxFloatRule(string propertyDisplayName, float minimum, float maximum)
    {
        this.minimum = minimum;
        this.maximum = maximum;
        this.propertyDisplayName = propertyDisplayName;
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is not string text)
        {
            return new ValidationResult(false, $"Must bind to {nameof(String)}");
        }

        if (!float.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var floatValue) || floatValue < minimum || floatValue > maximum)
        {
            return new ValidationResult(false, string.Format(Resources.MustBeBetween, propertyDisplayName, minimum, maximum));
        }

        return ValidationResult.ValidResult;
    }
}

public class MinMaxFloatRuleExtension : MarkupExtension
{
    public float Minimum { get; set; } = float.MinValue;
    public float Maximum { get; set; } = float.MaxValue;
    public string PropertyDisplayName { get; set; } = Resources.DefaultPropertyDisplayName;
    public override object ProvideValue(IServiceProvider serviceProvider) => new MinMaxFloatRule(PropertyDisplayName, Minimum, Maximum);
}

public class MinMaxIntRule : ValidationRule
{
    private readonly int minimum;
    private readonly int maximum;
    private readonly string propertyDisplayName;

    public MinMaxIntRule(string propertyDisplayName, int minimum, int maximum)
    {
        this.minimum = minimum;
        this.maximum = maximum;
        this.propertyDisplayName = propertyDisplayName;
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is not string text)
        {
            return new ValidationResult(false, $"Must bind to {nameof(String)}");
        }

        if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var intValue) || intValue < minimum || intValue > maximum)
        {
            return new ValidationResult(false, string.Format(Resources.MustBeBetween, propertyDisplayName, minimum, maximum));
        }

        return ValidationResult.ValidResult;
    }
}

public class MinMaxIntRuleExtension : MarkupExtension
{
    public int Minimum { get; set; } = 0;
    public int Maximum { get; set; } = 50000;
    public string PropertyDisplayName { get; set; } = Resources.DefaultPropertyDisplayName;
    public override object ProvideValue(IServiceProvider serviceProvider) => new MinMaxIntRule(PropertyDisplayName, Minimum, Maximum);
}

public class ChargingRuleDate : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is not string text)
        {
            return new ValidationResult(false, $"Must bind to {nameof(String)}");
        }

        var match = Gen24ChargingRule.TimeRegex.Match(text);
        var invalidTime = new ValidationResult(false, string.Format(Resources.InvalidChargingRuleTime, text));

        if (!match.Success)
        {
            return invalidTime;
        }

        var hours = byte.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minutes = byte.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        if (hours > 24 || minutes > 59 || hours == 24 && minutes != 0)
        {
            return invalidTime;
        }

        return ValidationResult.ValidResult;
    }
}

public class ChargingRuleDateExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new ChargingRuleDate();
}
