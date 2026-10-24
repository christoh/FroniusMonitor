using De.Hochstaetter.HomeAutomationClient.ViewModels;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// A view model that counts how often its <c>Initialize</c> was started, for the tests of what starts it:
/// <c>DialogView</c> for the dialog bodies, <c>UseViewModel</c> for the pages.
/// </summary>
/// <remarks>Public, because the MVVM toolkit's generator writes code for it that has to see it.</remarks>
public sealed class CountingViewModel : ViewModelBase
{
    public int InitializeCount { get; private set; }

    /// <summary>How many <c>DataContextChanged</c> handlers had run when <c>Initialize</c> was started.</summary>
    public int HandlersRunBeforeInitialize { get; private set; } = -1;

    public int HandlersRun { get; set; }

    public override Task Initialize()
    {
        InitializeCount++;
        HandlersRunBeforeInitialize = HandlersRun;
        return Task.CompletedTask;
    }
}
