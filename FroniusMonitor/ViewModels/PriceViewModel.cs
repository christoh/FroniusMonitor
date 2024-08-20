using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    public class PriceViewModel(IElectricityPriceService electricityPriceService) : ViewModelBase
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
                electricityPriceService.PropertyChanged += OnElectricityPriceServiceOnPropertyChanged;
                await RefreshPrices().ConfigureAwait(false);
            }
            else
            {
                timer = new(_ => RefreshPrices().GetAwaiter().GetResult(), null, TimeSpan.Zero, TimeSpan.FromMinutes(20));
            }
        }

        private void OnElectricityPriceServiceOnPropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            _ = RefreshPrices();
        }

        internal override async Task CleanUp()
        {
            try
            {
                electricityPriceService.PropertyChanged -= OnElectricityPriceServiceOnPropertyChanged;

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
            Prices = (await electricityPriceService.GetGetElectricityPricesAsync("de", 17.879, 1.2257).ConfigureAwait(false))?.ToArray() ?? throw new InvalidOperationException();
            Points = Prices.Select(p => new DataPoint(DateTimeAxis.ToDouble(p.StartTime.ToLocalTime().AddHours(.5)), (double)p.CentsPerKiloWattHour)).ToList();

            var model = new PlotModel
            {
                Title = "Electricity Prices",
                TitleFontWeight = 600,
                TitleFontSize = 20,
                Axes =
                {
                    new DateTimeAxis
                    {
                        //AbsoluteMinimum = Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.AddHours(2))).Min(),
                        //Minimum = Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.AddHours(2))).Min(),
                        //AbsoluteMaximum = Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.AddHours(1))).Max(),
                        //Maximum = Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.AddHours(1))).Max(),
                        IsZoomEnabled = false,
                        IsPanEnabled = false,
                        MinorIntervalType = DateTimeIntervalType.Hours,
                        IntervalType = DateTimeIntervalType.Hours,
                        IntervalLength = 50,
                        FontSize = 16,
                        PositionAtZeroCrossing = false,
                        MajorGridlineStyle = LineStyle.Solid,
                        MinorGridlineStyle = LineStyle.None
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
                        FontSize = 16
                    }
                }
            };

            var series = new LinearBarSeries { FillColor = OxyColors.LightGreen, BarWidth = 90, NegativeFillColor = OxyColors.Orange };
            series.Points.AddRange(Points);
            model.Series.Add(series);
            var lineSeries = new LineSeries { Color = OxyColors.Transparent, LabelFormatString = "{1:N2}" };
            lineSeries.Points.AddRange(Points);
            model.Series.Add(lineSeries);
            PlotModel = model;
        }
    }
}
