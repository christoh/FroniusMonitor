using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;
using De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// Solar.web's <c>GetChartNew</c> answers, cut down from what the live service sent on 2026-09-20, and the period
/// arithmetic the cache keys on.
/// </summary>
public sealed class SolarWebChartParserTests
{
    private const string PvSystemId = "318e321a-883b-4a82-868a-0fdc784476ad";

    private static readonly DateTime fetched = new(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>interval=month&amp;view=production, three of the six series, two days each.</summary>
    private const string MonthProduction = """
        {"isPremiumFeature":false,"navOptions":{"canDrillDown":true,"canMoveBackward":true,"canMoveForward":false},"settings":{"series":[
        {"type":"column","name":"Produktion (ohne Zähler)","stacking":"normal","data":[[1789862400000,5.79]],"index":3,"color":"#F7C002","fillOpacity":0,"yAxis":"kWh","id":"FromGenToSomewhere","threshold":0,"tooltip":{"valueSuffix":"kWh","valueDecimals":2},"dashStyle":"solid"},
        {"type":"column","name":"Energie ins Netz eingespeist","stacking":"normal","data":[[1788220800000,57.89],[1788307200000,70.05]],"index":1,"color":"#999999","fillOpacity":0,"yAxis":"kWh","id":"FromGenToGrid","threshold":0,"tooltip":{"valueSuffix":"kWh","valueDecimals":2},"dashStyle":"solid"},
        {"type":"column","name":"Energieprognose","stacking":"normal","data":[[1789862400000,32.69],[1789948800000,61.68]],"index":0,"color":{"pattern":{"id":"forecast-pattern","path":{"d":"M 0 10 L 10 0","stroke":"#f7c002","strokeWidth":2},"width":10,"height":10}},"fillOpacity":0,"yAxis":"kWh","id":"PvForecastTruncated","threshold":0,"tooltip":{"valueSuffix":"kWh","valueDecimals":2},"dashStyle":"solid","maxDate":"2026-09-22T09:15:00.0000000+02:00"}],
        "sumValue":"1.205,36 kWh","yAxis":[{"min":0.0,"id":"kWh"}],"xAxis":{"type":"datetime","max":1790726400000.0,"min":1788220800000.0,"tickInterval":86400000}},"specialView":"","title":"September 2026","showBattInfo":false,"sumValue":"1.205,36 kWh"}
        """;

    /// <summary>interval=day&amp;view=consumption: real instants five minutes apart, a state of charge in percent, and the battery state as a bubble with a text.</summary>
    private const string DayConsumption = """
        {"isPremiumFeature":false,"navOptions":{"canDrillDown":false,"canMoveBackward":true,"canMoveForward":true},"settings":{"series":[
        {"type":"spline","name":"Produktion","data":[[1789768800000,0.0],[1789769100000,null],[1789769400000,1196.0]],"index":7,"color":"#F7C002","yAxis":"W","marker":{"enabled":false,"radius":0},"id":"FromGen","threshold":0,"tooltip":{"valueSuffix":" W","valueDecimals":2},"lineWidth":2,"dashStyle":"solid"},
        {"type":"spline","name":"Ladezustand","data":[[1789768800000,79.0],[1789769100000,78.9]],"index":8,"color":"#3CAE2B","yAxis":"%","id":"StateOfCharge","threshold":0,"tooltip":{"valueSuffix":" %","valueDecimals":0},"lineWidth":2,"dashStyle":"solid"},
        {"type":"bubble","name":"Batteriestatus","data":[[1789853327000,73.1,"Normalbetrieb"]],"index":9,"color":"#3CAE2B","yAxis":"%","marker":{"enabled":true,"radius":5},"id":"BattOperatingState","threshold":0,"tooltip":{"valueSuffix":" ","pointFormatter":"function() { return this.z; }","valueDecimals":2},"lineWidth":0,"dashStyle":"solid"}],
        "sumValue":"6,53 kWh","yAxis":[{"min":0.0,"id":"W"},{"max":100.0,"min":0.0,"id":"%","opposite":true}],"xAxis":{"type":"datetime","max":1789855199000.0,"min":1789768800000.0}},"specialView":"","title":"19.09.2026","showBattInfo":true,"sumValue":"6,53 kWh"}
        """;

    [Fact]
    public void A_month_chart_has_one_column_per_day_stamped_at_midnight_utc()
    {
        var chart = SolarWebChartParser.Parse(MonthProduction, PvSystemId, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 1), fetched);

        Assert.Equal(PvSystemId, chart.PvSystemId);
        Assert.Equal(SolarWebInterval.Month, chart.Interval);
        Assert.Equal(SolarWebView.Production, chart.View);
        Assert.Equal(new DateOnly(2026, 9, 1), chart.Period);
        Assert.Equal(fetched, chart.FetchedUtc);
        Assert.False(chart.IsPremiumFeature);
        Assert.Equal("September 2026", chart.Title);
        Assert.Equal("1.205,36 kWh", chart.SumValue);
        Assert.Equal(["FromGenToSomewhere", "FromGenToGrid", "PvForecastTruncated"], chart.Series.Select(s => s.Id));
        Assert.All(chart.Series, s => Assert.Equal("kWh", s.Unit));
        Assert.All(chart.Series, s => Assert.Equal("column", s.ChartType));
        // The forecast comes with a hatch pattern instead of a colour; the stroke of the pattern is its colour.
        Assert.Equal(["#F7C002", "#999999", "#f7c002"], chart.Series.Select(s => s.Color));
        Assert.Equal([3, 1, 0], chart.Series.Select(s => s.Index));

        var grid = chart.Series[1];
        Assert.Equal("Energie ins Netz eingespeist", grid.Name);
        Assert.Equal(2, grid.Points.Count);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), grid.Points[0].TimeUtc);
        Assert.Equal(DateTimeKind.Utc, grid.Points[0].TimeUtc.Kind);
        Assert.Equal(57.89, grid.Points[0].Value);
        Assert.Equal(70.05, grid.Points[1].Value);
        Assert.Null(grid.Points[0].Text);

        // The forecast reaches into the future; it is a series like any other.
        Assert.Equal(new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc), chart.Series[2].Points[1].TimeUtc);
    }

    [Fact]
    public void A_day_chart_has_real_instants_a_missing_value_and_a_bubble_with_a_text()
    {
        var chart = SolarWebChartParser.Parse(DayConsumption, PvSystemId, SolarWebInterval.Day, SolarWebView.Consumption, new DateOnly(2026, 9, 19), fetched);

        Assert.Equal("19.09.2026", chart.Title);
        Assert.Equal("6,53 kWh", chart.SumValue);
        Assert.Equal(3, chart.Series.Count);

        var production = chart.Series[0];
        Assert.Equal("FromGen", production.Id);
        Assert.Equal("W", production.Unit);
        Assert.Equal("spline", production.ChartType);
        // Local midnight in Berlin, which is 22:00 UTC the evening before.
        Assert.Equal(new DateTime(2026, 9, 18, 22, 0, 0, DateTimeKind.Utc), production.Points[0].TimeUtc);
        Assert.Equal(TimeSpan.FromMinutes(5), production.Points[1].TimeUtc - production.Points[0].TimeUtc);
        Assert.Null(production.Points[1].Value);
        Assert.Equal(1196.0, production.Points[2].Value);

        Assert.Equal("%", chart.Series[1].Unit);
        Assert.Equal(78.9, chart.Series[1].Points[1].Value);

        var state = Assert.Single(chart.Series[2].Points);
        Assert.Equal("bubble", chart.Series[2].ChartType);
        Assert.Equal(73.1, state.Value);
        Assert.Equal("Normalbetrieb", state.Text);
        Assert.Equal(new DateTime(2026, 9, 19, 21, 28, 47, DateTimeKind.Utc), state.TimeUtc);
    }

    [Fact]
    public void The_premium_flag_and_a_series_without_data_are_kept_and_no_json_object_is_refused()
    {
        var chart = SolarWebChartParser.Parse("""{"isPremiumFeature":true,"settings":{"series":[{"id":"Saving","name":"Ersparnis","yAxis":"EUR","type":"column"}]},"title":"2026","sumValue":""}""", PvSystemId, SolarWebInterval.Year, SolarWebView.ReturnOfInvestment, new DateOnly(2026, 1, 1), fetched);

        Assert.True(chart.IsPremiumFeature);
        var series = Assert.Single(chart.Series);
        Assert.Equal("Saving", series.Id);
        Assert.Empty(series.Points);

        Assert.Throws<InvalidDataException>(() => SolarWebChartParser.Parse("[]", PvSystemId, SolarWebInterval.All, SolarWebView.Production, DateOnly.MinValue, fetched));
        Assert.Throws<InvalidDataException>(() => SolarWebChartParser.Parse("""{"settings":{"series":[{"id":"x","data":[[]]}]}}""", PvSystemId, SolarWebInterval.All, SolarWebView.Production, DateOnly.MinValue, fetched));
    }

    [Fact]
    public void A_period_is_named_by_its_first_day_and_ends_where_the_next_one_starts()
    {
        var date = new DateOnly(2026, 9, 19);

        Assert.Equal(date, SolarWebPeriod.Normalize(SolarWebInterval.Day, date));
        Assert.Equal(new DateOnly(2026, 9, 1), SolarWebPeriod.Normalize(SolarWebInterval.Month, date));
        Assert.Equal(new DateOnly(2026, 1, 1), SolarWebPeriod.Normalize(SolarWebInterval.Year, date));
        Assert.Equal(DateOnly.MinValue, SolarWebPeriod.Normalize(SolarWebInterval.All, date));

        Assert.Equal(new DateOnly(2026, 9, 20), SolarWebPeriod.End(SolarWebInterval.Day, date));
        Assert.Equal(new DateOnly(2026, 10, 1), SolarWebPeriod.End(SolarWebInterval.Month, new DateOnly(2026, 9, 1)));
        Assert.Equal(new DateOnly(2027, 1, 1), SolarWebPeriod.End(SolarWebInterval.Year, new DateOnly(2026, 1, 1)));
        Assert.Null(SolarWebPeriod.End(SolarWebInterval.All, DateOnly.MinValue));
    }

    [Fact]
    public void A_period_ends_at_local_midnight_and_has_a_period_before_it_except_the_whole_history()
    {
        var berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        // 2026-09-19 ends at 2026-09-20 00:00 Berlin, which is 2026-09-19 22:00 UTC in summer time; December in winter time.
        Assert.Equal(new DateTime(2026, 9, 19, 22, 0, 0, DateTimeKind.Utc), SolarWebPeriod.EndUtc(SolarWebInterval.Day, new DateOnly(2026, 9, 19), berlin));
        Assert.Equal(new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc), SolarWebPeriod.EndUtc(SolarWebInterval.Month, new DateOnly(2026, 9, 1), berlin));
        Assert.Equal(new DateTime(2026, 12, 31, 23, 0, 0, DateTimeKind.Utc), SolarWebPeriod.EndUtc(SolarWebInterval.Year, new DateOnly(2026, 1, 1), berlin));
        Assert.Null(SolarWebPeriod.EndUtc(SolarWebInterval.All, DateOnly.MinValue, berlin));

        Assert.Equal(new DateOnly(2026, 9, 18), SolarWebPeriod.Previous(SolarWebInterval.Day, new DateOnly(2026, 9, 19)));
        Assert.Equal(new DateOnly(2026, 8, 1), SolarWebPeriod.Previous(SolarWebInterval.Month, new DateOnly(2026, 9, 1)));
        Assert.Equal(new DateOnly(2025, 1, 1), SolarWebPeriod.Previous(SolarWebInterval.Year, new DateOnly(2026, 1, 1)));
        Assert.Null(SolarWebPeriod.Previous(SolarWebInterval.All, DateOnly.MinValue));
    }

    [Fact]
    public void The_maintenance_page_is_known_by_its_two_headings()
    {
        Assert.True(SolarWebClient.IsMaintenancePage("'Fronius': Maintenance Work We are currently optimizing our service for you"));
        Assert.True(SolarWebClient.IsMaintenancePage("Wartungsarbeiten Wir optimieren gerade unseren Service"));
        Assert.False(SolarWebClient.IsMaintenancePage("'Request Rejected': The requested URL was rejected."));
        Assert.False(SolarWebClient.IsMaintenancePage("'500 - Internal server error.': There is a problem with the resource you are looking for"));
    }

    [Fact]
    public void The_query_values_are_the_lower_case_names_and_the_premium_views_are_known()
    {
        Assert.Equal("day", SolarWebInterval.Day.ToQueryValue());
        Assert.Equal("all", SolarWebInterval.All.ToQueryValue());
        Assert.Equal("returnofinvestment", SolarWebView.ReturnOfInvestment.ToQueryValue());
        Assert.Equal("expense", SolarWebView.Expense.ToQueryValue());
        Assert.True(SolarWebView.ReturnOfInvestment.IsPremium());
        Assert.True(SolarWebView.Expense.IsPremium());
        Assert.False(SolarWebView.Production.IsPremium());
        Assert.False(SolarWebView.Consumption.IsPremium());
    }
}
