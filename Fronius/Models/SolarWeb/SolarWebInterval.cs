namespace De.Hochstaetter.Fronius.Models.SolarWeb;

/// <summary>
///     The four buttons at the bottom of Solar.web's chart page - Tag, Monat, Jahr, Gesamt - and the value of the
///     <c>interval</c> query parameter each of them sends, which is the lower case name.
/// </summary>
public enum SolarWebInterval
{
    /// <summary>One day in steps of five minutes; power in W, the state of charge in percent.</summary>
    Day,

    /// <summary>One month, one column per day; energy in kWh.</summary>
    Month,

    /// <summary>One year, one column per month; energy in kWh or MWh.</summary>
    Year,

    /// <summary>Everything, one column per year; energy in MWh.</summary>
    All,
}

/// <summary>
///     The four tabs at the top of the chart page - Produktion, Verbrauch, Rentabilität, Kosten - and the value of
///     the <c>view</c> query parameter each of them sends, which is the lower case name. The last two are Solar.web
///     Premium features and exist for a month, a year and the whole history, but not for a day: Solar.web answers
///     <c>500</c> to <c>interval=day</c> with either of them.
/// </summary>
public enum SolarWebView
{
    Production,

    Consumption,

    /// <summary>Rentabilität: what was earned by feeding in (Ertrag) and saved by not buying (Ersparnis), in EUR.</summary>
    ReturnOfInvestment,

    /// <summary>Kosten: what was saved (Ersparnis) and what buying from the grid actually cost (Tatsächliche Kosten), in EUR.</summary>
    Expense,
}

public static class SolarWebViewExtensions
{
    /// <summary>Whether the view is one of the two Premium features that Solar.web has no day chart for.</summary>
    public static bool IsPremium(this SolarWebView view) => view is SolarWebView.ReturnOfInvestment or SolarWebView.Expense;

    /// <summary>The value of the <c>view</c> query parameter.</summary>
    public static string ToQueryValue(this SolarWebView view) => view.ToString().ToLowerInvariant();

    /// <summary>The value of the <c>interval</c> query parameter.</summary>
    public static string ToQueryValue(this SolarWebInterval interval) => interval.ToString().ToLowerInvariant();
}
