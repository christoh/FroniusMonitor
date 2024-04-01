namespace De.Hochstaetter.FroniusMonitor.Models;

internal class GaugeBinding : Binding
{
    public GaugeBinding(string path) : base(path)
    {
        // The range base property 'Value' binds TwoWay by default which is not useful for a Gauge or ProgressIndicator.
        Mode = BindingMode.OneWay;

        // The range base property 'Value' throws exception if it is not typeof(double)
        FallbackValue = 0d;
        TargetNullValue = 0d;
        
        // Makes sense for virtually any binding
        ConverterCulture = CultureInfo.CurrentCulture;
    }

    public GaugeBinding() : this(null!) { }
}
