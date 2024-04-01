namespace De.Hochstaetter.FroniusMonitor.Models;

// ReSharper disable Xaml.PossibleNullReferenceException

internal class GaugeBinding : Binding
{
    public GaugeBinding(string path) : base(path)
    {
        TargetNullValue = 0d;
        Mode = BindingMode.OneWay;
        FallbackValue = 0d;
        ConverterCulture = CultureInfo.CurrentCulture;
    }

    public GaugeBinding() : this(null!) { }
}
