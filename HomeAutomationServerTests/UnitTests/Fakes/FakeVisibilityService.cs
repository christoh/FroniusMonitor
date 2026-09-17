using De.Hochstaetter.HomeAutomationClient.Contracts;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>A visibility the test sets by hand, for what the connection gate does with it.</summary>
public sealed class FakeVisibilityService : IVisibilityService
{
    private bool isAnyVisible = true;

    public bool IsAnyVisible
    {
        get => isAnyVisible;

        set
        {
            if (isAnyVisible == value)
            {
                return;
            }

            isAnyVisible = value;
            IsAnyVisibleChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? IsAnyVisibleChanged;
}
