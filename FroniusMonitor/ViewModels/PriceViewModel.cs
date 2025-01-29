using System.Text.Json;
using De.Hochstaetter.Fronius.Models.Settings;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace De.Hochstaetter.FroniusMonitor.ViewModels;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum ElectricityPriceDisplay
{
    Market = 0,
    Buy = 1,
    Sell = 2,
}

public class PriceViewModel(
    [SuppressMessage("ReSharper", "CapturedPrimaryConstructorParameterIsMutable")]
    IElectricityPriceService electricityPriceService,
    SettingsBase settings
) : ViewModelBase
{
    private const string LegendKey = "L";
    private const string EnergyAxisKey = "E";

    private Timer? timer;

    public IElectricityPriceService ElectricityPriceService => electricityPriceService;

    public IReadOnlyList<IElectricityPrice> Prices
    {
        get;
        set => Set(ref field, value);
    } = null!;

    public PlotModel? PlotModel
    {
        get;
        set => Set(ref field, value);
    }

    public bool UseVat
    {
        get;
        set => Set(ref field, value, OnSettingsChanged);
    } = true;

    public ElectricityPriceDisplay PriceDisplay
    {
        get;
        set => Set(ref field, value, OnSettingsChanged);
    } = ElectricityPriceDisplay.Buy;

    public bool ShowHistoricData
    {
        get;
        set => Set(ref field, value, OnSettingsChanged);
    }

    public DateTime Date
    {
        get;
        set => Set(ref field, value, OnSettingsChanged);
    } = DateTime.Now.Date;

    public BindableCollection<string> Errors
    {
        get;
        set => Set(ref field, value);
    } = [];

    public bool IsBusy
    {
        get;
        set => Set(ref field, value);
    }

    internal override async Task OnInitialize()
    {
        await base.OnInitialize().ConfigureAwait(false);

        if (ElectricityPriceService.IsPush)
        {
            ElectricityPriceService.PropertyChanged += OnServicePropertyChanged;
        }
        else
        {
            timer = new(_ => RefreshPrices().GetAwaiter().GetResult(), null, TimeSpan.Zero, TimeSpan.FromMinutes(20));
        }

        await RefreshPrices().ConfigureAwait(false);
        settings.ElectricityPrice.ElectricityPriceServiceChanged += OnElectricityPriceServiceChanged;
    }
        
    [SuppressMessage("ReSharper", "CapturedPrimaryConstructorParameterIsMutable")]
    private async void OnElectricityPriceServiceChanged(object? o, ElectricityPriceService service)
    {
        ElectricityPriceService.PropertyChanged -= OnServicePropertyChanged;
        electricityPriceService = IoC.Get<IElectricityPriceService>();
        ShowHistoricData = ElectricityPriceService.SupportsHistoricData && ShowHistoricData;
        NotifyOfPropertyChange(nameof(ElectricityPriceService));

        if (ElectricityPriceService.IsPush)
        {
            ElectricityPriceService.PropertyChanged += OnServicePropertyChanged;
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
            ElectricityPriceService.PropertyChanged -= OnServicePropertyChanged;
            settings.ElectricityPrice.ElectricityPriceServiceChanged -= OnElectricityPriceServiceChanged;

            if (timer != null)
            {
                await timer.DisposeAsync().ConfigureAwait(false);
            }

            timer = null;
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
        try
        {
            IsBusy = true;

            var (factor, offset, vatFactor) = GetFactorAndOffset();

            try
            {
                Prices = [];
                Errors.Clear();

                Prices = (await ElectricityPriceService.GetElectricityPricesAsync
                (
                    offset,
                    factor,
                    vatFactor,
                    ShowHistoricData ? Date : null,
                    ShowHistoricData ? Date.AddDays(1) : null
                ).ConfigureAwait(false)).ToArray() ?? throw new InvalidOperationException();
            }
            catch (JsonException)
            {
                Errors.Add($"{Loc.ElectricityPrice}: {Loc.NoValidData}");
            }
            catch (Exception ex)
            {
                Errors.Add($"{Loc.ElectricityPrice}: {ex.Message}");
            }

            IReadOnlyList<AwattarEnergy> energies = [];

            if (ElectricityPriceService is AwattarService awattarService)
            {
                try
                {
                    energies = (await awattarService.GetDataAsync
                    (
                        ShowHistoricData ? Date : null,
                        ShowHistoricData ? Date.AddDays(1) : Prices.Any() ? Prices[^1].EndTime : null
                    ).ConfigureAwait(false)).Energies;

                    await Settings.Save().ConfigureAwait(false);
                }
                catch (JsonException)
                {
                    Errors.Add($"{Loc.WindSolar}: {Loc.NoValidData}");
                }
                catch (Exception ex)
                {
                    Errors.Add($"{Loc.WindSolar}: {ex.Message}");
                }
            }

            const double adjustment = 1.000000001;
            const double margin = 1.1;
            var priceMin = Prices.Count == 0 ? 0 : Math.Min(Prices.Select(p => (double)p.CentsPerKiloWattHour).Min(), 0);
            var priceMax = Prices.Count == 0 ? 0 : Prices.Select(p => (double)p.CentsPerKiloWattHour).Max();

            var priceMinimum = priceMin > 0 ? priceMin / margin : priceMin * margin;
            var priceMaximum = priceMax + (priceMax - priceMinimum) * (energies.Count == 0 ? 0.1 : 0.6) * margin;

            var model = new PlotModel
            {
                Title = $"{Loc.ElectricityPrice} ({ElectricityPriceService.PriceRegion.ToDisplayName()})",
                TitleFontWeight = 600,
                TitleFontSize = 20,
                IsLegendVisible = true,

                Axes =
                {
                    new DateTimeAxis
                    {
                        AbsoluteMinimum = Prices.Count == 0 ? 0 : Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.ToLocalTime())).Min(),
                        Minimum = Prices.Count == 0 ? 0 : Prices.Select(p => DateTimeAxis.ToDouble(p.StartTime.ToLocalTime())).Min(),
                        AbsoluteMaximum = Prices.Count == 0 ? 1 : Prices.Select(p => DateTimeAxis.ToDouble(p.EndTime.ToLocalTime())).Max(),
                        Maximum = Prices.Count == 0 ? 1 : Prices.Select(p => DateTimeAxis.ToDouble(p.EndTime.ToLocalTime())).Max(),
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
                        Minimum = priceMinimum,
                        AbsoluteMinimum = priceMinimum,
                        Maximum = Math.Max(priceMaximum, priceMinimum + 1),
                        Unit = "ct/kWh",
                        AbsoluteMaximum = Math.Max(priceMaximum, priceMinimum + 1),
                        IsZoomEnabled = false,
                        IsPanEnabled = false,
                        FontSize = 16,
                        EdgeRenderingMode = EdgeRenderingMode.PreferSharpness,
                    },
                }
            };

            model.Legends.Add(new Legend
            {
                Key = LegendKey,
                IsLegendVisible = true,
                LegendPlacement = LegendPlacement.Outside,
                EdgeRenderingMode = EdgeRenderingMode.PreferSharpness,
                LegendBorderThickness = .5,
                LegendBorder = OxyColors.Black,
                LegendLineSpacing = 5,
            });

            var positivePrices = Prices.Where(p => p.CentsPerKiloWattHour >= 0m).ToList();

            if (positivePrices.Count > 0)
            {
                var positivePriceSeries = new RectangleBarSeries
                {
                    FillColor = OxyColors.LightSeaGreen,
                    StrokeThickness = 0.5,
                    LegendKey = LegendKey,
                    Title = Loc.ElectricityPrice
                };

                positivePrices.ForEach
                (
                    price => positivePriceSeries.Items.Add
                    (
                        new RectangleBarItem
                        (
                            DateTimeAxis.ToDouble(price.StartTime.ToLocalTime()),
                            0,
                            DateTimeAxis.ToDouble(price.EndTime.ToLocalTime()),
                            (double)price.CentsPerKiloWattHour
                        )
                    )
                );

                model.Series.Add(positivePriceSeries);
            }

            var negativePrices = Prices.Where(p => p.CentsPerKiloWattHour < 0m).ToList();

            if (negativePrices.Count > 0)
            {
                var negativePriceSeries = new RectangleBarSeries { FillColor = OxyColors.Coral, StrokeThickness = 0.5, LegendKey = LegendKey, Title = Loc.ElectricityPriceNegative };

                negativePrices.ForEach
                (
                    price => negativePriceSeries.Items.Add
                    (
                        new RectangleBarItem
                        (
                            DateTimeAxis.ToDouble(price.StartTime.ToLocalTime()),
                            0,
                            DateTimeAxis.ToDouble(price.EndTime.ToLocalTime()),
                            (double)price.CentsPerKiloWattHour
                        )
                    )
                );

                model.Series.Add(negativePriceSeries);
            }

            var lineSeriesPrice = new LineSeries { Color = OxyColors.Transparent, LabelFormatString = "{1:N2}", FontSize = 12, LabelMargin = 2 };
            lineSeriesPrice.Points.AddRange(Prices.Select(p => new DataPoint((DateTimeAxis.ToDouble(p.StartTime.ToLocalTime()) + DateTimeAxis.ToDouble(p.EndTime.ToLocalTime())) / 2, (double)p.CentsPerKiloWattHour)));
            model.Series.Add(lineSeriesPrice);

            if (ElectricityPriceService is AwattarService && energies.Count > 0)
            {
                var energyMin = -energies.Max(e => (e.SolarProductionMegaWatt + e.WindProductionMegaWatt) / 1000);

                model.Axes.Add(new LinearAxis
                {
                    Unit = "GW",
                    Position = AxisPosition.Right,
                    PositionAtZeroCrossing = false,
                    Minimum = energyMin * 3,
                    AbsoluteMinimum = energyMin * 3,
                    Maximum = 0,
                    AbsoluteMaximum = 0,
                    IsZoomEnabled = false,
                    IsPanEnabled = false,
                    StringFormat = "#,0;#,0",
                    Key = EnergyAxisKey,
                    FontSize = 16,
                    EdgeRenderingMode = EdgeRenderingMode.PreferSharpness,
                });

                var solarSeries = new RectangleBarSeries
                {
                    FillColor = OxyColor.FromArgb(255, 255, 0xd0, 0),
                    YAxisKey = EnergyAxisKey,
                    StrokeThickness = 0.5,
                    LegendKey = LegendKey,
                    Title = Loc.SolarProduction
                };

                energies.Apply
                (
                    s => solarSeries.Items.Add
                    (
                        new RectangleBarItem
                        (
                            DateTimeAxis.ToDouble(s.StartTime.ToLocalTime()),
                            0,
                            DateTimeAxis.ToDouble(s.EndTime.ToLocalTime()),
                            -s.SolarProductionMegaWatt / 1000
                        )
                    )
                );

                var windSeries = new RectangleBarSeries
                {
                    FillColor = OxyColors.DeepSkyBlue,
                    YAxisKey = EnergyAxisKey,
                    StrokeThickness = 0.5,
                    LegendKey = LegendKey,
                    Title = Loc.WindProduction
                };

                energies.Apply
                (
                    s => windSeries.Items.Add
                    (
                        new RectangleBarItem
                        (
                            DateTimeAxis.ToDouble(s.StartTime.ToLocalTime()),
                            -s.SolarProductionMegaWatt / 1000,
                            DateTimeAxis.ToDouble(s.EndTime.ToLocalTime()),
                            -(s.WindProductionMegaWatt + s.SolarProductionMegaWatt) / 1000
                        )
                    )
                );

                var lineSeriesProduction = new LineSeries
                {
                    Color = OxyColors.Transparent,
                    LabelFormatString = "{1:#.0;#.0}",
                    FontSize = 12,
                    LabelMargin = -17,
                    YAxisKey = EnergyAxisKey
                };

                lineSeriesProduction.Points.AddRange
                (
                    energies.Select
                    (
                        p => new DataPoint
                        (
                            (DateTimeAxis.ToDouble(p.StartTime.ToLocalTime()) + DateTimeAxis.ToDouble(p.EndTime.ToLocalTime())) / 2,
                            -(p.WindProductionMegaWatt + p.SolarProductionMegaWatt) / 1000)
                    )
                );

                model.Series.Add(solarSeries);
                model.Series.Add(windSeries);
                model.Series.Add(lineSeriesProduction);
            }

            PlotModel = model;
        }
        finally
        {
            IsBusy = false;
        }
    }
}