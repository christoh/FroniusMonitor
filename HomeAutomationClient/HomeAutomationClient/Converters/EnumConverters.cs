namespace De.Hochstaetter.HomeAutomationClient.Converters;

/// <summary>
/// An enum value as the localized text <c>Resources.&lt;Type&gt;_&lt;Name&gt;</c> holds for it, or its name where
/// there is none - the WPF app's <c>Enum2DisplayName</c>. Anything that is not an enum passes through.
/// </summary>
public class EnumDisplayName : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is Enum enumValue ? enumValue.ToDisplayName() : value;
}
