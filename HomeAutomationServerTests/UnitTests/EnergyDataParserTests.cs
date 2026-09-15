using System.Globalization;
using System.Xml.Linq;
using De.Hochstaetter.Fronius.Models.EnergyData;
using De.Hochstaetter.Fronius.Services.EnergyData;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The two DWD files as they are on opendata.dwd.de, cut down to a few values, and the numbers that have to come
/// out of them: kJ/m² per hour to W/m², Kelvin to Celsius, km/h to m/s, and the hour a value is filed under.
/// </summary>
public sealed class EnergyDataParserTests
{
    private const string Kml = """
        <?xml version="1.0" encoding="ISO-8859-1" standalone="yes"?>
        <kml:kml xmlns:dwd="https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd" xmlns:kml="http://www.opengis.net/kml/2.2">
            <kml:Document>
                <kml:ExtendedData>
                    <dwd:ProductDefinition>
                        <dwd:Issuer>Deutscher Wetterdienst</dwd:Issuer>
                        <dwd:ProductID>MOSMIX</dwd:ProductID>
                        <dwd:IssueTime>2026-09-13T09:00:00.000Z</dwd:IssueTime>
                        <dwd:ForecastTimeSteps>
                            <dwd:TimeStep>2026-09-13T10:00:00.000Z</dwd:TimeStep>
                            <dwd:TimeStep>2026-09-13T11:00:00.000Z</dwd:TimeStep>
                            <dwd:TimeStep>2026-09-13T12:00:00.000Z</dwd:TimeStep>
                        </dwd:ForecastTimeSteps>
                    </dwd:ProductDefinition>
                </kml:ExtendedData>
                <kml:Placemark>
                    <kml:name>10870</kml:name>
                    <kml:description>MUENCHEN-FL.</kml:description>
                    <kml:ExtendedData>
                        <dwd:Forecast dwd:elementName="TTT">
                            <dwd:value>   288.05   -   290.15</dwd:value>
                        </dwd:Forecast>
                        <dwd:Forecast dwd:elementName="Rad1h">
                            <dwd:value>   1170.00    1800.00    3600.00</dwd:value>
                        </dwd:Forecast>
                        <dwd:Forecast dwd:elementName="FF">
                            <dwd:value>   4.63   4.12   -</dwd:value>
                        </dwd:Forecast>
                        <dwd:Forecast dwd:elementName="N">
                            <dwd:value>   75.00   50.00   25.00</dwd:value>
                        </dwd:Forecast>
                    </kml:ExtendedData>
                </kml:Placemark>
            </kml:Document>
        </kml:kml>
        """;

    private const string Csv =
        "surface observations;Parameter description;cloud_cover_total;dry_bulb_temperature_at_2_meter_above_ground;global_radiation_last_hour;mean_wind_speed_during last_10_min_at_10_meters_above_ground\n" +
        "10870;Unit;%;Grad C;W/m2;km/h\n" +
        "13.09.26;15:00;100;22,5;---;12\n" +
        "13.09.26;14:00;88;21,0;350;18\n" +
        "13.09.26;13:30;88;21,0;300;18\n";

    [Fact]
    public void The_mosmix_forecast_files_each_element_under_its_hour_and_converts_the_units()
    {
        var forecast = DwdMosmixParser.ParseKml(XDocument.Parse(Kml));

        Assert.Equal("10870", forecast.StationId);
        Assert.Equal("MUENCHEN-FL.", forecast.StationName);
        Assert.Equal(new DateTime(2026, 9, 13, 9, 0, 0, DateTimeKind.Utc), forecast.IssueTime);
        Assert.All(forecast.Points, p => Assert.True(p.IsForecast));

        // Rad1h of the 10:00 step is the hour before it, so the first slot is 09:00 - and it holds nothing else.
        var nine = Assert.Single(forecast.Points, p => p.Time == Utc(9));
        Assert.Equal(1170.0 * 1000 / 3600, nine.GlobalRadiationWattsPerSquareMeter!.Value, 6);
        Assert.Null(nine.TemperatureCelsius);
        Assert.Null(nine.WindSpeedMetersPerSecond);

        var ten = Assert.Single(forecast.Points, p => p.Time == Utc(10));
        Assert.Equal(500, ten.GlobalRadiationWattsPerSquareMeter!.Value, 6);
        Assert.Equal(288.05 - 273.15, ten.TemperatureCelsius!.Value, 6);
        Assert.Equal(4.63, ten.WindSpeedMetersPerSecond!.Value, 6);
        Assert.Equal(75, ten.CloudCoverPercent);

        // A missing value is "-" and stays missing rather than becoming zero.
        var eleven = Assert.Single(forecast.Points, p => p.Time == Utc(11));
        Assert.Null(eleven.TemperatureCelsius);
        Assert.Equal(1000, eleven.GlobalRadiationWattsPerSquareMeter!.Value, 6);

        var twelve = Assert.Single(forecast.Points, p => p.Time == Utc(12));
        Assert.Null(twelve.WindSpeedMetersPerSecond);
        Assert.Null(twelve.GlobalRadiationWattsPerSquareMeter);
        Assert.Equal(290.15 - 273.15, twelve.TemperatureCelsius!.Value, 6);
    }

    [Fact]
    public void The_observation_file_is_read_by_column_name_and_the_wind_becomes_meters_per_second()
    {
        var points = DwdObservationParser.Parse(new StringReader(Csv));

        Assert.All(points, p => Assert.False(p.IsForecast));

        // 14:00's radiation is the mean of 13:00 to 14:00, so it lands on the 13:00 slot; the wind of 14:00 stays there.
        var thirteen = Assert.Single(points, p => p.Time == Utc(13));
        Assert.Equal(350, thirteen.GlobalRadiationWattsPerSquareMeter);
        Assert.Null(thirteen.WindSpeedMetersPerSecond);

        var fourteen = Assert.Single(points, p => p.Time == Utc(14));
        Assert.Equal(18 / 3.6, fourteen.WindSpeedMetersPerSecond!.Value, 6);
        Assert.Equal(21.0, fourteen.TemperatureCelsius);
        Assert.Equal(88, fourteen.CloudCoverPercent);
        Assert.Null(fourteen.GlobalRadiationWattsPerSquareMeter); // "---" at 15:00

        var fifteen = Assert.Single(points, p => p.Time == Utc(15));
        Assert.Equal(22.5, fifteen.TemperatureCelsius);
        Assert.Equal(12 / 3.6, fifteen.WindSpeedMetersPerSecond!.Value, 6);

        // The half hour row is not an hourly mean and is left out.
        Assert.DoesNotContain(points, p => p.Time.Minute != 0);
        Assert.Equal(3, points.Count);
    }

    [Fact]
    public void The_observation_parser_survives_a_culture_with_a_decimal_comma()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var points = DwdObservationParser.Parse(new StringReader(Csv));
            Assert.Equal(22.5, Assert.Single(points, p => p.Time == Utc(15)).TemperatureCelsius);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Prices_are_displayed_the_way_the_invoice_adds_them_up()
    {
        List<EnergyPriceComponent> components =
        [
            new() { Name = "Netz", NetPrice = 4m, TaxRate = 0.19m },
            new() { Name = "Umlage", NetPrice = 1m, TaxRate = 0m },
            new() { Name = "Grundpreis", NetPrice = 98.55m, TaxRate = 0.19m, Unit = "Euro/Jahr" },
        ];

        Assert.Equal(10m, EnergyPriceCalculator.Display(10m, components, 0.19m, EnergyPriceDisplay.Market, gross: false));
        Assert.Equal(11.9m, EnergyPriceCalculator.Display(10m, components, 0.19m, EnergyPriceDisplay.Market, gross: true));
        Assert.Equal(15m, EnergyPriceCalculator.Display(10m, components, 0.19m, EnergyPriceDisplay.Buy, gross: false));
        Assert.Equal(11.9m + 4.76m + 1m, EnergyPriceCalculator.Display(10m, components, 0.19m, EnergyPriceDisplay.Buy, gross: true));

        // A negative market price is taxed like a positive one.
        Assert.Equal(-2.38m, EnergyPriceCalculator.Display(-2m, components, 0.19m, EnergyPriceDisplay.Market, gross: true));
    }

    [Fact]
    public void A_day_counts_as_covered_only_when_the_slots_add_up_to_it()
    {
        var day = new DateTime(2026, 9, 13, 22, 0, 0, DateTimeKind.Utc);
        var hourly = Enumerable.Range(0, 24).Select(i => new EnergyPricePoint { StartTime = day.AddHours(i), EndTime = day.AddHours(i + 1) }).ToList();
        var quarterly = Enumerable.Range(0, 96).Select(i => new EnergyPricePoint { StartTime = day.AddMinutes(15 * i), EndTime = day.AddMinutes(15 * (i + 1)) }).ToList();

        Assert.True(EnergyPriceCalculator.CoversSpan(hourly, day, day.AddDays(1)));
        Assert.True(EnergyPriceCalculator.CoversSpan(quarterly, day, day.AddDays(1)));
        Assert.False(EnergyPriceCalculator.CoversSpan(hourly.Take(23), day, day.AddDays(1)));
        Assert.False(EnergyPriceCalculator.CoversSpan([], day, day.AddDays(1)));

        // A slot that reaches beyond the span only counts for the part inside it.
        Assert.False(EnergyPriceCalculator.CoversSpan(hourly.Skip(1), day, day.AddDays(1)));
        Assert.True(EnergyPriceCalculator.CoversSpan(hourly.Concat(hourly.Select(p => new EnergyPricePoint { StartTime = p.StartTime.AddDays(1), EndTime = p.EndTime.AddDays(1) })), day, day.AddDays(1)));
    }

    private static DateTime Utc(int hour) => new(2026, 9, 13, hour, 0, 0, DateTimeKind.Utc);
}
