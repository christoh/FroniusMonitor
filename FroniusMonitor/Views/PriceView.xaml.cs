using System.Collections.Immutable;

namespace De.Hochstaetter.FroniusMonitor.Views;

public partial class PriceView
{
    private readonly PriceViewModel viewModel;

    public PriceView(PriceViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.OnInitialize().ConfigureAwait(false);
        Closed += async (_, _) => await viewModel.CleanUp().ConfigureAwait(false);
    }

    protected override ScaleTransform Scaler => ScaleTransform;

    private void OnConsumptionViewChecked(object sender, RoutedEventArgs e) => viewModel.PriceDisplay = ElectricityPriceDisplay.Buy;

    private void OnMarketViewChecked(object sender, RoutedEventArgs e) => viewModel.PriceDisplay = ElectricityPriceDisplay.Market;

    private void DecreaseDate(object sender, RoutedEventArgs e) => AddDays(-1);
    private void IncreaseDate(object sender, RoutedEventArgs e) => AddDays(1);

    private void AddDays(double days)
    {
        if (DatePicker.SelectedDate is { } date)
        {
            DatePicker.SelectedDate = date.AddDays(days);
        }
    }

    private static readonly DateTime minDate = new(2013, 12, 22);

    private void SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DatePicker.SelectedDate is not { } date)
        {
            return;
        }

        var maxDate = DateTime.Now.Hour > 12 ? DateTime.Now.AddDays(1).Date : DateTime.Now.Date;

        IncreaseButton.IsEnabled = date < maxDate;
        DecreaseButton.IsEnabled = date > minDate;
    }

    private void OnPriceComponentsClick(object sender, RoutedEventArgs e)
    {
        if (viewModel.ElectricityPriceService is AwattarService service)
        {
            new PriceComponentsView((service.Prices ?? []).Where(p => p.PriceUnit == "Cent/kWh" && p.Price!=0m).ToList()).Show();
        }
    }
}
