using De.Hochstaetter.Fronius.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Converters
{
    public abstract class ConverterBase : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TreeViewConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IHierarchicalCollection hierarchicalCollection))
            {
                return null!;
            }

            var result = new CompositeCollection
            {
                new CollectionContainer { Collection = hierarchicalCollection.ItemsEnumerable },
                new CollectionContainer { Collection = hierarchicalCollection.ChildrenEnumerable },
            };

            return result;
        }
    }
}
