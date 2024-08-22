using De.Hochstaetter.Fronius.Models.Settings;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class PriceViewModel(IElectricityPriceService electricityPriceService, SettingsBase settings) : ViewModelBase
    {
        private IReadOnlyList<IElectricityPrice> prices = null!;
        private Timer? timer;

        public IReadOnlyList<IElectricityPrice> Prices
        {
            get => prices;
            set => Set(ref prices, value);
        }

        private List<DataPoint> points = null!;

        public List<DataPoint> Points
        {
            get => points;
            set => Set(ref points, value);
        }

        private PlotModel? plotModel;

        public PlotModel? PlotModel
        {
            get => plotModel;
            set => Set(ref plotModel, value);
        }

        internal override async Task OnInitialize()
        {
            await base.OnInitialize().ConfigureAwait(false);

            if (electricityPriceService.IsPush)
            {
                electricityPriceService.PropertyChanged += OnServicePropertyChanged;
            }
            else
            {
                timer = new(_ => RefreshPrices().GetAwaiter().GetResult(), null, TimeSpan.Zero, TimeSpan.FromMinutes(20));
            }

            await RefreshPrices().ConfigureAwait(false);
            settings.ElectricityPrice.ElectricityPriceServiceChanged += OnElectricityPriceServiceChanged;
        }

        private async void OnElectricityPriceServiceChanged(object? o, ElectricityPriceService service)
        {
            electricityPriceService.PropertyChanged -= OnServicePropertyChanged;
            electricityPriceService = IoC.Get<IElectricityPriceService>();

            if (electricityPriceService.IsPush)
            {
                electricityPriceService.PropertyChanged += OnServicePropertyChanged;
            }
            else
            {
                await RefreshPrices().ConfigureAwait(false);

                if (timer != null)
                {
                    await timer.DisposeAsync().ConfigureAwait(false);
                }

                timer = new(_ => RefreshPrices().GetAwaiter().GetResult(), null, TimeSpan.Zero, TimeSpan.FromMinutes(20));
            }
        }

        private void OnServicePropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IElectricityPriceService.RawValues):
                    _ = RefreshPrices();
                    break;
            }
        }

        internal override async Task CleanUp()
        {
            try
            {
                electricityPriceService.PropertyChanged -= OnServicePropertyChanged;
                settings.ElectricityPrice.ElectricityPriceServiceChanged -= OnElectricityPriceServiceChanged;

                if (timer != null)
                {
                    await timer.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await base.CleanUp().ConfigureAwait(false);
            }
        }

        private async Task RefreshPrices()
        {
            Prices = (await electricityPriceService.GetGetElectricityPricesAsync
            (
                settings.ElectricityPrice.PriceOffsetBuy * settings.ElectricityPrice.VatFactor,
                settings.ElectricityPrice.PriceFactorBuy * settings.ElectricityPrice.VatFactor
            ).ConfigureAwait(false)).ToArray() ?? throw new InvalidOperationException();

            Points = Prices.Select(p => new DataPoint(DateTimeAxis.ToDouble(p.StartTime.ToLocalTime().AddHours(.5)), (double)p.CentsPerKiloWattHour)).ToList();

            const double adjustment = 1.000000001;

            var model = new PlotModel
            {
                Title = $"{Loc.ElectricityPrice} ({electricityPriceService.PriceRegion.ToDisplayName()})",
                TitleFontWeight = 600,
                TitleFontSize = 20,

                Axes =
                {
                    new DateTimeAxis
                    {
                        AbsoluteMinimum = Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.ToLocalTime())).Min(),
                        Minimum = Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.ToLocalTime())).Min(),
                        AbsoluteMaximum = Prices.Select(p => DateTimeAxis.ToDouble(p.EndTime.ToLocalTime())).Max(),
                        Maximum = Prices.Select(p => DateTimeAxis.ToDouble(p.EndTime.ToLocalTime())).Max(),
                        IsZoomEnabled = false,
                        IsPanEnabled = false,
                        IntervalType = DateTimeIntervalType.Days,
                        MajorStep = 2 / 24d * adjustment, MinorStep = 1 / 24d * adjustment,
                        EdgeRenderingMode = EdgeRenderingMode.PreferSharpness,
                        StringFormat = "HH:mm",
                        IntervalLength = 48,
                        FontSize = 14,
                        PositionAtZeroCrossing = false,
                        MajorGridlineStyle = LineStyle.Solid,
                        MinorGridlineStyle = LineStyle.Solid,
                    },
                    new LinearAxis
                    {
                        Position = AxisPosition.Left,
                        PositionAtZeroCrossing = false,
                        Minimum = Points.Select(p => p.Y).Min() - 5,
                        AbsoluteMinimum = Points.Select(p => p.Y).Min() - 5,
                        Maximum = Points.Select(p => p.Y).Max() + 5,
                        AbsoluteMaximum = Points.Select(p => p.Y).Max() + 5,
                        IsZoomEnabled = false,
                        IsPanEnabled = false,
                        FontSize = 16,
                        EdgeRenderingMode = EdgeRenderingMode.PreferSharpness,
                    }
                }
            };

            var series = new LinearBarSeries { FillColor = OxyColors.LightGreen, BarWidth = 90, NegativeFillColor = OxyColors.Orange };
            series.Points.AddRange(Points);
            model.Series.Add(series);

            var lineSeries = new LineSeries { Color = OxyColors.Transparent, LabelFormatString = "{1:N2}", FontSize = 10, LabelMargin = 2, FontWeight = 600 };
            lineSeries.Points.AddRange(Points);
            model.Series.Add(lineSeries);
            PlotModel = model;
        }
    }
}
