namespace De.Hochstaetter.Fronius.Models;

public partial class AwattarEnergyList : BindableBase
{
    [ObservableProperty]
    [JsonPropertyName("data")]
    public partial List<AwattarEnergy> Energies { get; set; } = [];

    [ObservableProperty]
    [JsonIgnore]
    public partial IEnumerable<AwattarPriceComponent>? Prices { get; set; }

    /// <summary>
    /// The hours that carry a forecast, in order. The hours beyond Awattar's horizon come back with null values
    /// (see <see cref="AwattarEnergy"/>) and are left out: a forecast that does not exist is not zero megawatts.
    /// </summary>
    /// <remarks>
    /// It sits on the list and not on whoever fetched it, because both heads that ask Awattar themselves - the
    /// server through its <c>AwattarClient</c> and the WPF app through its own <c>AwattarService</c> - need the
    /// same conversion, and <see cref="GridProductionPoint"/> is what the rest of the solution reads.
    /// </remarks>
    public IReadOnlyList<GridProductionPoint> ToProductions() => Energies
        .Where(e => e.HasValues)
        .Select(e => new GridProductionPoint
        {
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            SolarMegaWatt = e.SolarProductionMegaWatt!.Value,
            WindMegaWatt = e.WindProductionMegaWatt!.Value,
        })
        .OrderBy(p => p.StartTime)
        .ToList();
}