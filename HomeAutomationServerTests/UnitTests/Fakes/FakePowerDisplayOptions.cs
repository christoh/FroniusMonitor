using System.ComponentModel;
using De.Hochstaetter.HomeAutomationClient.Contracts;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>The Solar Web switch of the main view, thrown by hand: what the house block and the power flow page make of it.</summary>
public sealed class FakePowerDisplayOptions : IPowerDisplayOptions
{
    private bool includeInverterPower;

    public bool IncludeInverterPower
    {
        get => includeInverterPower;

        set
        {
            if (includeInverterPower == value)
            {
                return;
            }

            includeInverterPower = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IncludeInverterPower)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
