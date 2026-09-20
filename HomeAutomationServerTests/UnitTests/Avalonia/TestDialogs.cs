using Avalonia.Controls;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Controls;
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

/// <summary>Parameters that ask for a modal window, the way a message box does, for a <see cref="TestDialog"/>.</summary>
public sealed class ModalTestDialogParameters : DialogParameters
{
    public override bool IsModalWindow => true;
}

/// <summary>A body that says how big its window opens, the way the power flow page does.</summary>
public sealed class SizedTestDialogView : ContentControl, IDialogControl
{
    public const double InitialWidth = 700;
    public const double InitialHeight = 500;

    public SizedTestDialogView()
    {
        InitialWindowSize.SetWidth(this, InitialWidth);
        InitialWindowSize.SetHeight(this, InitialHeight);
    }
}

/// <summary><see cref="TestDialog"/> with a <see cref="SizedTestDialogView"/> for a body.</summary>
public sealed class SizedTestDialog(DialogParameters parameters) : DialogBase<DialogParameters, bool, SizedTestDialogView>(parameters)
{
    public override Task AbortAsync()
    {
        Result = false;
        Close();
        return Task.CompletedTask;
    }
}

/// <summary>A detail page that knows which device it was given.</summary>
public sealed class TestPage : ContentControl
{
    public string? DeviceKey { get; set; }
}
