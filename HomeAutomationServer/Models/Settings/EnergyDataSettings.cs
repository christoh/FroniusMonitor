namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
///     The sources of the price chart, as the server's <c>Settings.xml</c> holds them: the Awattar account of the
///     installation and the DWD station whose weather is shown.
/// </summary>
/// <remarks>
///     <para>
///         None of this is a secret in the sense the passwords of the other connections are. The bearer token is
///         Awattar's fixed token for the tariff query, it has been the same for years and it only reads a public
///         tariff, so it is stored in clear text; the postal code and the grid operator id select the tariff and
///         are printed on every invoice.
///     </para>
///     <para>
///         The market prices and the sun and wind productions need no token at all, so an element with an empty
///         <see cref="Bearer" /> still collects them; only the price components stay empty.
///     </para>
/// </remarks>
[XmlType("EnergyData")]
public partial class EnergyDataSettings : BindableBase
{
    /// <summary>Awattar's bearer token for <c>/v1/prices</c>. Empty means the price components are not read.</summary>
    [ObservableProperty, XmlAttribute, DefaultValue("")]
    public partial string Bearer { get; set; } = string.Empty;

    /// <summary>The postal code the tariff is looked up for.</summary>
    [ObservableProperty, XmlAttribute, DefaultValue("")]
    public partial string PostalCode { get; set; } = string.Empty;

    /// <summary>The grid operator (Netzbetreiber) id of the installation, 13 digits.</summary>
    [ObservableProperty, XmlAttribute, DefaultValue("")]
    public partial string GridOperatorId { get; set; } = string.Empty;

    /// <summary>
    ///     What Awattar charges for its service, cents per kWh net. It is not part of the tariff answer, so it is
    ///     configured here; 1.5 ct/kWh is what they charge as of 2026.
    /// </summary>
    [ObservableProperty, XmlAttribute, DefaultValue(typeof(decimal), "1.5")]
    public partial decimal SurchargeCentsPerKiloWattHour { get; set; } = 1.5m;

    /// <summary>The VAT rate that applies to the market price and to the surcharge, in percent.</summary>
    [ObservableProperty, XmlAttribute, DefaultValue(typeof(decimal), "19")]
    public partial decimal VatRatePercent { get; set; } = 19;

    /// <summary>The price zone. Awattar serves Germany/Luxembourg and Austria.</summary>
    [ObservableProperty, XmlAttribute, DefaultValue(AwattarCountry.GermanyLuxembourg)]
    public partial AwattarCountry PriceRegion { get; set; } = AwattarCountry.GermanyLuxembourg;

    /// <summary>
    ///     The DWD station whose forecast and measurements are shown, by the id of the MOSMIX station catalogue
    ///     (<c>https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_stationskatalog.cfg</c>). Empty means no
    ///     weather is collected. 10863 is Weihenstephan, which measures global radiation as well; not every station does.
    /// </summary>
    [ObservableProperty, XmlAttribute, DefaultValue("")]
    public partial string DwdStationId { get; set; } = string.Empty;

    /// <summary>
    ///     The time zone the polling windows and the day boundaries are meant in, as an IANA or Windows id. Empty
    ///     means the time zone of the machine, which in a container is UTC unless <c>TZ</c> is set.
    /// </summary>
    [ObservableProperty, XmlAttribute, DefaultValue("")]
    public partial string TimeZoneId { get; set; } = string.Empty;

    /// <summary>The VAT rate as a factor, 0.19 for 19 %.</summary>
    [XmlIgnore]
    public decimal VatRate => VatRatePercent / 100;

    /// <summary>Whether Awattar can be asked for the price components at all.</summary>
    [XmlIgnore]
    public bool HasTariffQuery => Bearer.Length > 0 && PostalCode.Length > 0 && GridOperatorId.Length > 0;

    /// <inheritdoc cref="TimeZones.Resolve" />
    public TimeZoneInfo ResolveTimeZone(ILogger? logger = null) => TimeZones.Resolve(TimeZoneId, logger);
}
