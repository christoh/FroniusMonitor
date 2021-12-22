using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using De.Hochstaetter.Fronius.Contracts;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Converters
{
    public abstract class ConverterBase : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

        public virtual object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TreeViewConverter : ConverterBase
    {
        public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is IHierarchicalCollection hierarchicalCollection))
            {
                return null!;
            }

            var result = new CompositeCollection
            {
                new CollectionContainer {Collection = hierarchicalCollection.ItemsEnumerable},
                new CollectionContainer {Collection = hierarchicalCollection.ChildrenEnumerable},
            };

            return result;
        }
    }

    public class NullToString : ConverterBase
    {
        public string NullText { get; init; } = "---";

        public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value ?? NullText;
        }
    }

    public class PowerDirectionToAnything<T> : ConverterBase
    {
        public T? Incoming { get; set; }
        public T? Outgoing { get; set; }
        public T? Null { get; set; } = default;

        public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not IConvertible convertible)
            {
                return Null;
            }

            var doubleVal = convertible.ToDouble(culture);
            return doubleVal < 0 ? Outgoing : doubleVal > 0 ? Incoming : Null;
        }
    }

    public class PowerDirectionToDouble : PowerDirectionToAnything<double>
    { }

    public class PowerDirectionToThickness:PowerDirectionToAnything<Thickness>{}

    public class ToAbsolute : ConverterBase
    {
        public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not IConvertible convertible)
            {
                return null;
            }

            return Math.Abs(convertible.ToDouble(culture));
        }
    }
}
