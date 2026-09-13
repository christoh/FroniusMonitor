namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Dialogs;

/// <summary>
/// The tariff components behind the buying price, ported from <c>PriceComponentsView</c> of FroniusMonitor: one
/// row per component with its net price, VAT rate and gross price, and the two sums below. Only the components
/// that are a price per kWh, as the WPF table shows them; a monthly base fee is not part of a price per kWh.
/// </summary>
public sealed partial class PriceComponentsViewModel(DialogParameters parameters, IEnumerable<EnergyPriceComponent> components)
    : DialogBase<DialogParameters, bool, PriceComponentsView>(parameters)
{
    public IReadOnlyList<EnergyPriceComponent> Components { get; } = [.. components.Where(c => c.IsPerKiloWattHour && c.Name.Length > 0)];

    public decimal NetSum => Components.Sum(c => c.NetPrice);

    public decimal GrossSum => Components.Sum(c => c.GrossPrice);

    public string Unit => Components.FirstOrDefault()?.Unit ?? EnergyPriceComponent.CentsPerKiloWattHourUnit;

    [RelayCommand]
    private void CloseDialog()
    {
        Result = true;
        Close();
    }

    public override Task AbortAsync()
    {
        CloseDialog();
        return Task.CompletedTask;
    }
}
