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

public class Null2Bool : Null2AnythingBase<bool>
{
    public Null2Bool()
    {
        Null = false;
        NotNull = true;
    }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? Null : NotNull;
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
