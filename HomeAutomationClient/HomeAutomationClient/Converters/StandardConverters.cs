using System.Collections;

namespace De.Hochstaetter.HomeAutomationClient.Converters;

public class BoolInverter : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : null;
    }
}

public abstract class Null2AnythingBase<T> : ConverterBase
{
    public T? Null { get; set; }
    public T? NotNull { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? Null : NotNull;
    }
}

public class Null2ColorMap : Null2AnythingBase<IReadOnlyList<ColorThreshold>>;

public class Null2Double : Null2AnythingBase<double>;


public class Null2Bool : Null2AnythingBase<bool>
{
    public Null2Bool()
    {
        Null = false;
        NotNull = true;
    }
}

public abstract class Any2AnythingBase<T> : ConverterBase
{
    public T? Any { get; set; }
    public T? Empty { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ICollection { Count: > 1, } ? Any : Empty;
    }
}

public class Any2String : Any2AnythingBase<string> { }

public class Any2Thickness : Any2AnythingBase<Thickness> { }

public abstract class Bool2AnythingBase<T> : ConverterBase
{
    public T? True { get; set; }
    public T? False { get; set; }
    public T? Null { get; set; }
    
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? (b ? True : False) : Null;
    }
}

public class Bool2Char : Bool2AnythingBase<char>
{
    public Bool2Char()
    {
        True = '\0';
        False = '•';
        Null = '•';
    }
}

public class Bool2Double : Bool2AnythingBase<double>;

public class DateConverter : ConverterBase
{
    public string StringFormat { get; set; } = "G";
    public bool UseUtc { get; set; } = false;
    public bool UseCurrentCulture { get; set; } = true;

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime date)
        {
            return null;
        }

        return (UseUtc ? date.ToUniversalTime() : date.ToLocalTime()).ToString(StringFormat, UseCurrentCulture ? CultureInfo.CurrentCulture : culture);
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var effectiveCulture = UseCurrentCulture ? CultureInfo.CurrentCulture : culture;

        return DateTime.TryParse
        (
            value is IConvertible convertible ? convertible.ToString(effectiveCulture) : value?.ToString(),
            effectiveCulture,
            UseUtc ? DateTimeStyles.AssumeUniversal : DateTimeStyles.AssumeLocal,
            out var date
        )
            ? date
            : value;
    }
}

