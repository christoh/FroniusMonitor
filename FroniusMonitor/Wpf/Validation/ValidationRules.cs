﻿namespace De.Hochstaetter.FroniusMonitor.Wpf.Validation;

internal class StringResult(Func<string> func) : IHaveDisplayName
{
    public override string ToString() => func();
    public string DisplayName => func();

    public static ValidationResult Create(Func<string> localFunc) => new(false, new StringResult(localFunc));
}

public class MarkupRule(ValidationRuleExtension extension) : ValidationRule
{
    public override ValidationResult Validate(object? value, CultureInfo cultureInfo) => extension.Validate(value);
}

public abstract class ValidationRuleExtension : MarkupExtension
{
    private readonly MarkupRule rule;

    protected ValidationRuleExtension()
    {
        rule = new MarkupRule(this);
    }

    public sealed override object ProvideValue(IServiceProvider serviceProvider) => rule;
    public abstract ValidationResult Validate(object? value);
}

internal class RegExRuleExtension : ValidationRuleExtension
{
    public string Message { get; set; } = string.Empty;
    public string Pattern { get; set; } = @"^.*$";

    public override ValidationResult Validate(object? value)
    {
        return !Regex.IsMatch(value?.ToString() ?? string.Empty, Pattern)
            ? new ValidationResult(false, new StringResult(() => Message))
            : ValidationResult.ValidResult;
    }
}

public class Ipv4OrHostnameExtension : ValidationRuleExtension
{
    public override ValidationResult Validate(object? value)
    {
        var stringValue = value?.ToString();
        var hostnameType = Uri.CheckHostName(stringValue);

        var result = hostnameType switch
        {
            UriHostNameType.IPv6 => StringResult.Create(() => Loc.NoHostnameOrIpv4Address),
            UriHostNameType.Unknown => StringResult.Create(() => Loc.NoHostnameOrIpv4Address),
            _ => ValidationResult.ValidResult,
        };
        
        return result;
    }
}

public class MinMaxFloatRuleExtension : ValidationRuleExtension
{
    public float Minimum { get; set; } = float.MinValue;
    public float Maximum { get; set; } = float.MaxValue;
    public string PropertyDisplayName { get; set; } = Loc.DefaultPropertyDisplayName;

    public override ValidationResult Validate(object? value)
    {
        return !float.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var floatValue) || floatValue < Minimum || floatValue > Maximum
            ? StringResult.Create(() => string.Format(Loc.MustBeBetween, PropertyDisplayName, Minimum, Maximum))
            : ValidationResult.ValidResult;
    }
}

public class MinMaxIntRuleExtension : ValidationRuleExtension
{
    public int Minimum { get; set; } = 0;
    public int Maximum { get; set; } = 50000;
    public string PropertyDisplayName { get; set; } = Loc.DefaultPropertyDisplayName;

    public override ValidationResult Validate(object? value)
    {
        return int.TryParse(value?.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var intValue) && intValue >= Minimum && intValue <= Maximum
            ? ValidationResult.ValidResult
            : StringResult.Create(() => string.Format(Loc.MustBeBetween, PropertyDisplayName, Minimum, Maximum));
    }
}

public class WattPilotFallbackCurrentRuleExtension : ValidationRuleExtension
{
    public string PropertyDisplayName { get; set; } = Loc.DefaultPropertyDisplayName;

    public override ValidationResult Validate(object? value)
    {
        return int.TryParse(value?.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var intValue) && intValue is >= 6 and <= 32 or 0
            ? ValidationResult.ValidResult
            : StringResult.Create(() => string.Format(Loc.FallbackCurrentError, PropertyDisplayName));
    }
}

public class ChargingRuleDateExtension : ValidationRuleExtension
{
    public override ValidationResult Validate(object? value)
    {
        if (value is not string text)
        {
            return new ValidationResult(false, $"Must bind to {nameof(String)}");
        }

        var match = Gen24ChargingRule.TimeRegex().Match(text);
        var invalidTime = StringResult.Create(() => string.Format(Loc.InvalidChargingRuleTime, text));

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
