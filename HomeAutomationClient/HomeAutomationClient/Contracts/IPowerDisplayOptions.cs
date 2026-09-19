namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// How the powers of the house are to be read. The user's choice, not the devices': nothing here changes what a
/// device reports, only which of it is called the consumption of the house. Nothing here is a UI type, so a view
/// model may ask; the answer is the main view's switch, and <c>MainViewModel</c> is what holds it.
/// </summary>
public interface IPowerDisplayOptions : INotifyPropertyChanged
{
    /// <summary>
    /// "Solar Web" mode: the power an inverter loses on its way from the panels to the AC side counts as house
    /// consumption, the way Fronius' own portal reports it. Off by default, which is what the inverter itself
    /// says - it reports the load without its own loss.
    /// </summary>
    /// <remarks>
    /// The loss is <c>Gen24PowerFlow.PowerLoss</c>, and it is added to the consumption and to nothing else: the
    /// loss stays on the dashboard as its own figure, and self-sufficiency and own consumption are worked out
    /// from the house's real consumption, so that they do not jump when the switch is thrown. The WPF app has
    /// the same setting under the name <c>AddInverterPowerToConsumption</c>, where it is saved with the settings;
    /// here it lives as long as the app does, like the other two switches of the main view.
    /// </remarks>
    bool IncludeInverterPower { get; }
}
