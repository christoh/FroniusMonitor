using De.Hochstaetter.Fronius.Models.Settings;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace De.Hochstaetter.FroniusMonitor.ViewModels
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum ElectricityPriceDisplay
    {
        Market = 0,
        Buy = 1,
        Sell = 2,
    }

    public class PriceViewModel(IElectricityPriceService electricityPriceService, SettingsBase settings) : ViewModelBase
    {
        private IReadOnlyList<IElectricityPrice> prices = null!;
        private Timer? timer;

        public IElectricityPriceService ElectricityPriceService => electricityPriceService;

        public IReadOnlyList<IElectricityPrice> Prices
        {
            get => prices;
            set => Set(ref prices, value);
        }

        private IReadOnlyList<DataPoint> pricePoints = null!;

        public IReadOnlyList<DataPoint> PricePoints
        {
            get => pricePoints;
            set => Set(ref pricePoints, value);
        }

        private PlotModel? plotModel;

        public PlotModel? PlotModel
        {
            get => plotModel;
            set => Set(ref plotModel, value);
        }

        private bool useVat = true;

        public bool UseVat
        {
            get => useVat;
            set => Set(ref useVat, value, OnSettingsChanged);
        }

        private ElectricityPriceDisplay priceDisplay = ElectricityPriceDisplay.Buy;

        public ElectricityPriceDisplay PriceDisplay
        {
            get => priceDisplay;
            set => Set(ref priceDisplay, value, OnSettingsChanged);
        }

        private bool showHistoricData;

        public bool ShowHistoricData
        {
            get => showHistoricData;
            set => Set(ref showHistoricData, value, OnSettingsChanged);
        }

        private DateTime date = DateTime.Now.Date;

        public DateTime Date
        {
            get => date;
            set => Set(ref date, value, OnSettingsChanged);
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
            NotifyOfPropertyChange(nameof(ElectricityPriceService));

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

        private void OnSettingsChanged()
        {
            _ = RefreshPrices();
        }

        private (decimal Factor, decimal Offset, decimal VatFactor) GetFactorAndOffset()
        {
            return
            (
                PriceDisplay switch
                {
                    ElectricityPriceDisplay.Buy => settings.ElectricityPrice.PriceFactorBuy,
                    ElectricityPriceDisplay.Market => 1,
                    _ => throw new NotSupportedException()
                },
                PriceDisplay switch
                {
                    ElectricityPriceDisplay.Buy => settings.ElectricityPrice.PriceOffsetBuy,
                    ElectricityPriceDisplay.Market => 0,
                    _ => throw new NotSupportedException()
                },
                UseVat ? settings.ElectricityPrice.VatFactor : 1
            );
        }

        private async Task RefreshPrices()
        {
            var (factor, offset, vatFactor) = GetFactorAndOffset();

            Prices = (await electricityPriceService.GetElectricityPricesAsync
            (
                offset,
                factor,
                vatFactor,
                ShowHistoricData ? Date : null,
                ShowHistoricData ? Date.AddDays(1) : null
            ).ConfigureAwait(false)).ToArray() ?? throw new InvalidOperationException();

            IReadOnlyList<AwattarEnergy> energies = [];

            if (electricityPriceService is AwattarService awattarService)
            {
                energies = (await awattarService.GetHistoricEnergyDataAsync
                (
                    ShowHistoricData ? Date : null,
                    ShowHistoricData ? Date.AddDays(1) : null
                ).ConfigureAwait(false)).ToList();
            }

            PricePoints = Prices.Select(p => new DataPoint(DateTimeAxis.ToDouble(p.StartTime.ToLocalTime().AddHours(.5)), (double)p.CentsPerKiloWattHour)).ToList();

            const double adjustment = 1.000000001;
            const double margin = 1.1;
            var priceMin = PricePoints.Select(p => p.Y).Min();
            var priceMax = PricePoints.Select(p => p.Y).Max();

            var priceMinimum = priceMin > 0 ? priceMin / margin : priceMin * margin;
            var priceMaximum = priceMax + (priceMax - priceMinimum) * 0.6 * margin;

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
                        Minimum = priceMin > 0 ? priceMin / margin : priceMin * margin,
                        AbsoluteMinimum = priceMinimum,
                        Maximum = priceMaximum,
                        AbsoluteMaximum = priceMaximum,
                        IsZoomEnabled = false,
                        IsPanEnabled = false,
                        FontSize = 16,
                        EdgeRenderingMode = EdgeRenderingMode.PreferSharpness,
                    },
                }
            };

            var series = new LinearBarSeries { FillColor = OxyColors.LightGreen, BarWidth = 90, NegativeFillColor = OxyColors.Orange };
            series.Points.AddRange(PricePoints);
            model.Series.Add(series);

            var lineSeries = new LineSeries { Color = OxyColors.Transparent, LabelFormatString = "{1:N2}", FontSize = 10, LabelMargin = 2 };
            lineSeries.Points.AddRange(PricePoints);
            model.Series.Add(lineSeries);

            if (electricityPriceService is AwattarService)
            {
                var energyMin = -energies.Max(e => (e.SolarProduction + e.WindProduction) / 1000);

                model.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Right,
                    PositionAtZeroCrossing = false,
                    Minimum = energyMin * 3,
                    AbsoluteMinimum = energyMin * 3,
                    Maximum = 0,
                    AbsoluteMaximum = 0,
                    IsZoomEnabled = false,
                    IsPanEnabled = false,
                    Key = "Energy",
                    FontSize = 16,
                    EdgeRenderingMode = EdgeRenderingMode.PreferSharpness,
                });

                var solar = new RectangleBarSeries { FillColor = OxyColor.FromArgb(255, 255, 0xd0, 0), YAxisKey = "Energy", };

                energies.Apply
                (
                    s => solar.Items.Add
                    (
                        new RectangleBarItem
                        (
                            DateTimeAxis.ToDouble(s.StartTime.ToLocalTime()),
                            0,
                            DateTimeAxis.ToDouble(s.EndTime.ToLocalTime()),
                            -s.SolarProduction / 1000
                        )
                    )
                );

                var wind = new RectangleBarSeries { FillColor = OxyColors.DodgerBlue, YAxisKey = "Energy", };

                energies.Apply
                (
                    s => wind.Items.Add
                    (
                        new RectangleBarItem
                        (
                            DateTimeAxis.ToDouble(s.StartTime.ToLocalTime()),
                            -s.SolarProduction / 1000,
                            DateTimeAxis.ToDouble(s.EndTime.ToLocalTime()),
                            (-s.SolarProduction - s.WindProduction) / 1000)
                    )
                );

                model.Series.Add(solar);
                model.Series.Add(wind);
            }

            PlotModel = model;
        }
    }
}
