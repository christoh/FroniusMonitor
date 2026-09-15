using Avalonia.Controls;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models.Dialogs;
using De.Hochstaetter.HomeAutomationClient.ViewModels.Adapters;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>The body of <see cref="TestDialog"/>: as little as the dialog system accepts.</summary>
/// <remarks>
/// It declares a maximum width, because a dialog states one for the size it wants to have and a resizable window
/// has to be able to pass it.
/// </remarks>
public sealed class TestDialogView : ContentControl, IDialogControl
{
    public TestDialogView() => MaxWidth = 500;
}

/// <summary>
/// A dialog that does nothing, so that what the dialog system does around it can be seen on its own.
/// </summary>
public sealed class TestDialog(DialogParameters parameters) : DialogBase<DialogParameters, bool, TestDialogView>(parameters)
{
    /// <summary>How often the close box - of the frame or of the window chrome - has ended this dialog.</summary>
    public int AbortCount { get; private set; }

    public override Task AbortAsync()
    {
        AbortCount++;
        Result = false;
        Close();
        return Task.CompletedTask;
    }

    /// <summary>What an Ok button would do.</summary>
    public void Accept()
    {
        Result = true;
        Close();
    }

    public void SetBusyText(string? text) => BusyText = text;

    public string? ReadBusyText() => BusyText;
}

/// <summary>A detail page that knows which device it was given.</summary>
public sealed class TestPage : ContentControl
{
    public string? DeviceKey { get; set; }
}
