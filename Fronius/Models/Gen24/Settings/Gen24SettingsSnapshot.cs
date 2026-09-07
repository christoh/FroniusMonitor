namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

/// <summary>
/// Everything the inverter settings dialog needs, read from one inverter in a single round trip.
/// </summary>
/// <remarks>
/// <para>
/// The dialog asks for this once when it opens, so all four tabs work from the same moment in time. It does not
/// matter if it then goes stale: the server reads the inverter again before it writes anything, so the change it
/// applies is a delta against what the inverter really holds and not against what this snapshot happened to say.
/// </para>
/// <para>
/// <see cref="Gen24Config"/> covers some of the same ground, but it is what the data collectors keep per inverter
/// and carries no Modbus settings and no charging rules. This type exists for the dialog and travels over the wire.
/// </para>
/// </remarks>
public class Gen24SettingsSnapshot
{
    /// <summary>
    /// The common settings. Carries <see cref="Gen24InverterSettings.Mppt"/>,
    /// <see cref="Gen24InverterSettings.PowerLimitSettings"/> and <see cref="Gen24InverterSettings.AcSystemSettings"/>
    /// with it, so the inverter settings tab needs nothing else.
    /// </summary>
    public Gen24InverterSettings? InverterSettings { get; set; }

    public Gen24BatterySettings? BatterySettings { get; set; }

    /// <summary>
    /// The time of use rules. A plain list rather than a <c>BindableCollection</c>: that one needs a
    /// <see cref="SynchronizationContext"/>, which does not survive a trip through JSON. The view model wraps it.
    /// </summary>
    public List<Gen24ChargingRule> ChargingRules { get; set; } = [];

    public Gen24ModbusSettings? ModbusSettings { get; set; }

    /// <summary>The firmware versions, keyed the way the inverter reports them - "GEN24" among them.</summary>
    public IDictionary<string, Version> SoftwareVersions { get; set; } = new Dictionary<string, Version>();

    /// <summary>The nominal AC power of the inverter, which the self consumption tab needs for its limits.</summary>
    public double? MaxAcPower { get; set; }
}
