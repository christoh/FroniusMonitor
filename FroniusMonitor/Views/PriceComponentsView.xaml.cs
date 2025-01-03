using System.Runtime.CompilerServices;

namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class PriceComponentsView : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public IEnumerable<AwattarPriceComponent>? PriceComponents { get; private set; }
    public decimal GrossSum => PriceComponents?.Sum(c => c.GrossPrice) ?? decimal.Zero;
    public decimal NetSum => PriceComponents?.Sum(c => c.Price) ?? decimal.Zero;

    public PriceComponentsView()
    {
        InitializeComponent();

        Loaded += async (s, e) =>
        {
            if (IoC.TryGet<IElectricityPriceService>() is AwattarService awattarService)
            {
                _ = await awattarService.GetDataAsync(DateTime.Today, DateTime.Today.AddHours(23)).ConfigureAwait(false);
                PriceComponents = (awattarService.Prices ?? []).Where(p => p.PriceUnit == "Cent/kWh" && !string.IsNullOrEmpty(p.Name)).ToList();
                NotifyOfPropertyChange(nameof(PriceComponents));
                NotifyOfPropertyChange(nameof(GrossSum));
                NotifyOfPropertyChange(nameof(NetSum));
            }

            return;

            void NotifyOfPropertyChange(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        };
    }

    protected override ScaleTransform Scaler => WindowScaler;
}